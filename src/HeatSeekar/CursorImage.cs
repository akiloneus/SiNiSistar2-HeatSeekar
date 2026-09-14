using System.Buffers.Binary;
using BepInEx.Logging;
using UnityEngine;

namespace HeatSeekar;

internal sealed class CursorImage : IDisposable
{
    private const int MaximumBytes = 16 * 1024 * 1024;
    private const int MaximumDimension = 4096;
    private readonly string root;
    private readonly byte[] builtIn;
    private readonly ManualLogSource log;
    private LoadedImage? external;
    private LoadedImage? fallback;
    private string? configuredPath;
    private string? lastWarning;
    private (DateTime Write, long Length) stamp;
    private float nextCheck;
    private bool retry;
    internal bool UsingFallback => external == null;

    internal CursorImage(string directory, string defaultPath, string resource, ManualLogSource log)
    {
        root = directory;
        this.log = log;
        using var stream = typeof(CursorImage).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing built-in image: {resource}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        builtIn = buffer.ToArray();
        var destination = Path.Combine(root, defaultPath);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (!File.Exists(destination))
            {
                using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write);
                output.Write(builtIn);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { Warn($"Could not create the default PNG; the built-in image is still available: {destination}. {error.Message}"); }
    }

    internal Sprite Get(string path, float time)
    {
        var changed = configuredPath != path;
        if (changed || time >= nextCheck)
        {
            configuredPath = path;
            nextCheck = time + 1f;
            try
            {
                var file = new FileInfo(Path.GetFullPath(path, root));
                var currentStamp = (file.LastWriteTimeUtc, file.Exists ? file.Length : -1);
                if (changed || retry || stamp != currentStamp)
                {
                    stamp = currentStamp;
                    retry = false;
                    if (!file.Exists) throw new FileNotFoundException("PNG does not exist.", file.FullName);
                    if (file.Length > MaximumBytes) throw new InvalidDataException($"PNG must be smaller than {MaximumBytes / 1024 / 1024} MiB.");
                    var replacement = Decode(File.ReadAllBytes(file.FullName));
                    external?.Dispose();
                    external = replacement;
                    lastWarning = null;
                }
            }
            catch (Exception error)
            {
                // A file being saved or locked may become readable without a
                // timestamp change. Retry IO failures; decode failures wait for an edit.
                retry = error is UnauthorizedAccessException or IOException;
                external?.Dispose();
                external = null;
                Warn($"Could not load PNG; using the centered built-in image: {path}. {error.Message}");
            }
        }
        return (external ?? (fallback ??= Decode(builtIn))).Sprite;
    }

    private static LoadedImage Decode(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature)
            || BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4)) != 13
            || !bytes.AsSpan(12, 4).SequenceEqual(new byte[] { 73, 72, 68, 82 }))
            throw new InvalidDataException("The image must be a PNG with a valid IHDR header.");
        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
        if (width is 0 or > MaximumDimension || height is 0 or > MaximumDimension)
            throw new InvalidDataException($"PNG dimensions must be between 1 and {MaximumDimension} pixels.");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidDataException("Could not decode the PNG.");
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return new LoadedImage(texture, sprite);
        }
        catch
        {
            UnityEngine.Object.Destroy(texture);
            throw;
        }
    }

    private void Warn(string message)
    {
        if (lastWarning == message) return;
        lastWarning = message;
        log.LogWarning(message);
    }

    public void Dispose()
    {
        external?.Dispose();
        fallback?.Dispose();
        external = null;
        fallback = null;
        configuredPath = null;
    }

    private sealed class LoadedImage : IDisposable
    {
        private readonly Texture2D texture;
        internal Sprite Sprite { get; }
        internal LoadedImage(Texture2D texture, Sprite sprite) { this.texture = texture; Sprite = sprite; }
        public void Dispose()
        {
            UnityEngine.Object.Destroy(Sprite);
            UnityEngine.Object.Destroy(texture);
        }
    }
}

using UnityEngine;

namespace HeatSeekar;

internal sealed class AimVisuals : IDisposable
{
    private readonly Dictionary<string, Texture2D> textures = new();
    private readonly Dictionary<IntPtr, Sprite> replacements = new();
    private readonly Dictionary<SpriteRenderer, Sprite> originals = new();
    private readonly VisualMask points = new();
    private Transform? root;
    private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
    private Transform[] aimPoints = Array.Empty<Transform>();
    private float nextScan;

    internal void Apply(Transform? owner, bool enabled)
    {
        if (!enabled || owner == null) { Restore(); return; }
        if (root != owner) { Restore(); root = owner; nextScan = 0; }
        if (Time.realtimeSinceStartup >= nextScan)
        {
            nextScan = Time.realtimeSinceStartup + 0.25f;
            renderers = owner.GetComponentsInChildren<SpriteRenderer>(true);
            aimPoints = owner.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == "AimPoint" && item.parent != null && item.parent.name == "Bow").ToArray();
        }
        points.Apply(aimPoints);
        foreach (var renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null) continue;
            var sprite = renderer.sprite;
            if (sprite.name is not ("magic_bow_effect_003" or "magic_bow_effect_004")) continue;
            originals[renderer] = sprite;
            if (!replacements.TryGetValue(sprite.Pointer, out var replacement))
            {
                var texture = Texture(sprite.name);
                replacement = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height), sprite.pixelsPerUnit);
                replacement.name = "HeatSeekar.AimHidden." + sprite.name;
                replacement.hideFlags = HideFlags.HideAndDontSave;
                replacements.Add(sprite.Pointer, replacement);
            }
            renderer.sprite = replacement;
        }
    }

    private Texture2D Texture(string name)
    {
        if (textures.TryGetValue(name, out var existing)) return existing;
        using var stream = typeof(AimVisuals).Assembly.GetManifestResourceStream("HeatSeekar.Assets." + name + ".png")
            ?? throw new InvalidOperationException("Missing aim-point replacement: " + name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(texture, buffer.ToArray(), false)) throw new InvalidOperationException("Invalid aim-point replacement: " + name);
        texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        textures.Add(name, texture);
        return texture;
    }

    private void Restore()
    {
        points.Dispose();
        foreach (var entry in originals)
            if (entry.Key != null && entry.Key.sprite != null && entry.Key.sprite.name.StartsWith("HeatSeekar.AimHidden.", StringComparison.Ordinal))
                entry.Key.sprite = entry.Value;
        originals.Clear(); root = null; renderers = Array.Empty<SpriteRenderer>(); aimPoints = Array.Empty<Transform>();
    }

    public void Dispose()
    {
        Restore();
        foreach (var sprite in replacements.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
        foreach (var texture in textures.Values) if (texture != null) UnityEngine.Object.Destroy(texture);
        replacements.Clear(); textures.Clear();
    }
}

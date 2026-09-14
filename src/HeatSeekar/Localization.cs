using System.Text;
using System.Text.Json;
using BepInEx.Logging;
using SiNiSistar2.Lc;
using SiNiSistar2.Manager;
using UnityEngine;

namespace HeatSeekar;

internal sealed class Localization
{
    private readonly string root;
    private readonly ManualLogSource log;
    private readonly Dictionary<string, Dictionary<string, string>> defaults = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> preparedFiles = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> current = new();
    private string identity = "";
    private float nextCheck;
    private DateTime lastWrite;
    private readonly HashSet<string> warnings = new();
    public string Language { get; private set; } = "en";
    public Font? Font { get; private set; }

    public Localization(ManualLogSource log, string? directory = null)
    {
        this.log = log;
        root = directory ?? Path.Combine(PluginData.Root, "localization");
        foreach (var code in new[] { "en", "ja", "zh-CN", "zh-TW", "ko" })
        {
            using var resource = typeof(Localization).Assembly.GetManifestResourceStream($"HeatSeekar.Localization.{code}.json")
                ?? throw new InvalidOperationException($"Missing built-in language: {code}");
            using var reader = new StreamReader(resource, Encoding.UTF8);
            var text = reader.ReadToEnd();
            defaults[code] = Parse(text);
            templates[code] = text;
        }
        foreach (var code in defaults.Keys) EnsureFile(code);
        Load("en");
    }

    public void Tick()
    {
        var managers = GameContext.Managers;
        var localize = managers != null ? managers.m_Localize : null;
        if (localize == null || !ManagerList.HasCompletedFirstInitialize || localize.IsLoadingLanguageIndex) return;
        var checkFiles = Time.realtimeSinceStartup >= nextCheck;
        if (checkFiles)
        {
            nextCheck = Time.realtimeSinceStartup + 1f;
            var directories = localize.ModLanguageDirectoryInfos;
            if (directories != null)
            {
                var count = directories.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<LanguageDirectoryInfo>>().Count;
                for (var index = 0; index < count; index++)
                    EnsureFile(LanguageFolder(directories[index]));
            }
        }
        var code = localize.LanguageType switch
        {
            LanguageType.Japanese => "ja", LanguageType.English => "en",
            LanguageType.SimplifiedChinese => "zh-CN", LanguageType.TraditionalChinese => "zh-TW",
            LanguageType.Korean => "ko", _ => LanguageFolder(localize.GetCurrentLanguageDirectoryInfo())
        };
        if (!ValidCode(code)) code = "en";
        var state = localize.LanguageIndex + ":" + code;
        if (state != identity)
        {
            EnsureFile(code);
            identity = state;
            Font = localize.GetLcFont();
            Load(code);
        }
        if (!checkFiles) return;
        var write = File.GetLastWriteTimeUtc(FilePath(Language));
        if (write != lastWrite) Load(Language);
    }

    private static string LanguageFolder(LanguageDirectoryInfo? info)
    {
        if (info == null || info.m_IsBuiltin) return "";
        return Path.GetFileName((info.m_ModDirectoryPath ?? "").TrimEnd('/', '\\'));
    }

    private string FallbackCode(string code)
    {
        if (defaults.ContainsKey(code)) return code;
        var normalized = code.Replace('-', '_').ToUpperInvariant();
        if (normalized.Contains("ZH_TW") || normalized.Contains("ZH_HANT")) return "zh-TW";
        if (normalized.Contains("ZH_CN") || normalized.Contains("ZH_HANS")) return "zh-CN";
        if (normalized.EndsWith("JP") || normalized.EndsWith("JA")) return "ja";
        if (normalized.EndsWith("KR") || normalized.EndsWith("KO")) return "ko";
        return "en";
    }

    private void EnsureFile(string code)
    {
        if (!ValidCode(code) || preparedFiles.Contains(code)) return;
        var file = FilePath(code);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            if (!File.Exists(file))
            {
                using var output = new StreamWriter(new FileStream(file, FileMode.CreateNew, FileAccess.Write), new UTF8Encoding(false));
                output.Write(templates[FallbackCode(code)]);
            }
            preparedFiles.Add(code);
        }
        catch (IOException error) { Warn(file, error); }
        catch (UnauthorizedAccessException error) { Warn(file, error); }
    }

    internal void Load(string code)
    {
        if (!ValidCode(code)) code = "en";
        Language = code;
        // A mod folder keeps its identity even when it uses a built-in fallback.
        current = new(defaults[FallbackCode(code)]);
        var file = FilePath(code);
        lastWrite = File.GetLastWriteTimeUtc(file);
        if (!File.Exists(file)) return;
        try
        {
            var entries = Parse(File.ReadAllText(file));
            // Apply aliases first so an explicit semantic key wins regardless
            // of JSON property order. Existing files remain user-owned.
            foreach (var pair in entries.Where(pair => LocalizationKeys.Resolve(pair.Key) != pair.Key))
                if (!IsRetiredDefault(code, pair.Key, pair.Value)) current[LocalizationKeys.Resolve(pair.Key)] = pair.Value;
            foreach (var pair in entries.Where(pair => LocalizationKeys.Resolve(pair.Key) == pair.Key))
                current[pair.Key] = pair.Value;
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
        { Warn(file, error); }
    }

    // Existing JSON is user-owned. Recognize only exact defaults shipped by
    // earlier builds; every other override and the file itself stay untouched.
    private bool IsRetiredDefault(string code, string key, string value)
    {
        if (!defaults.ContainsKey(code)) return false;
        if (key == "HeatSeekar" && value == "HeatSeekar") return true;
        if (code == "ja" && key == "Aki-style menu navigation" && value == "Aki スタイルのメニューナビゲーション") return true;
        if (LocalizationKeys.Resolve(key) == "ui_help_preserve_aspect" && value == (code switch
        {
            "en" => "Recommended. Preserve the game's original 2:1 aspect ratio at any resolution. Unused space is black.",
            "ja" => "有効化を推奨。どの解像度でもゲーム本来の2:1の縦横比を維持し、余白は黒く表示します。",
            "zh-CN" => "建议开启。在任何分辨率下保持游戏原本的 2:1 画面比例，剩余区域显示为黑边。",
            "zh-TW" => "建議開啟。在任何解析度下保持遊戲原本的 2:1 畫面比例，剩餘區域顯示為黑邊。",
            "ko" => "활성화를 권장합니다. 모든 해상도에서 게임 본래의 2:1 화면 비율을 유지하며, 남는 영역은 검게 표시합니다.",
            _ => null
        })) return true;
        return key == "Fullscreen" && value == (code switch
        {
            "zh-CN" => "独占全屏", "zh-TW" => "獨佔全螢幕", "ko" => "독점 전체 화면", _ => null
        });
    }

    public string Text(string key)
    {
        var canonical = LocalizationKeys.Resolve(key);
        return current.TryGetValue(canonical, out var value) ? value
            : defaults["en"].TryGetValue(canonical, out value) ? value : key;
    }
    public string Format(string key, params object[] arguments)
    {
        try { return string.Format(Text(key), arguments); }
        catch (FormatException) { return string.Format(defaults["en"].TryGetValue(LocalizationKeys.Resolve(key), out var value) ? value : key, arguments); }
    }

    private string FilePath(string code) => Path.Combine(root, code, "ui.json");
    private static bool ValidCode(string code) => !string.IsNullOrWhiteSpace(code)
        && code.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && code is not "." and not "..";
    private static Dictionary<string, string> Parse(string json)
    {
        using var document = JsonDocument.Parse(json.TrimStart('\uFEFF'));
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("A language file must contain a flat JSON object.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateObject())
            if (item.Value.ValueKind != JsonValueKind.String || !result.TryAdd(item.Name, item.Value.GetString()!))
                throw new InvalidDataException("Language keys must be unique and values must be strings.");
        return result;
    }

    private void Warn(string file, Exception error)
    {
        if (warnings.Add(file + error.Message)) log.LogWarning($"Localization file kept unchanged; using built-in text where needed: {file}. {error.Message}");
    }
}

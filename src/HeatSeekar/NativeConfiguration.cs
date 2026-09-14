using System.Text.Json;
using BepInEx.Logging;
using SiNiSistar2;
using SiNiSistar2.Manager;
using UnityEngine.InputSystem;

namespace HeatSeekar;

// The game owns its existing profile. HeatSeekar applies a delta to the real
// gameplay action map and masks that delta at the native serialization boundary.
internal sealed class NativeConfiguration : IDisposable
{
    internal sealed record SaveState(string Keys, VideoSetting Video);
    private readonly ManualLogSource log;
    private SiNiSistar2.InputManager? input;
    private InputActionMap? map;
    private SystemSaveData? system;
    private Dictionary<string, JsonElement> baseline = new();
    private string baselineKeys = "", baselineRuntime = "", lastRuntime = "";
    private bool baselineFullscreen, baselineVsync, initialized;
    private int baselineResolution;
    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    internal bool Ready => initialized;

    public NativeConfiguration(ManualLogSource log) => this.log = log;

    public void Tick()
    {
        if (initialized) return;
        var managers = GameContext.Managers;
        if (managers == null || !ManagerList.HasCompletedFirstInitialize || managers.m_SaveData?.m_SystemSaveData == null
            || managers.m_InputManager?.InputObjectList == null) return;
        input = managers.m_InputManager;
        InputAction? action = null;
        foreach (var item in input.InputObjectList)
            if (item?.InputData?.m_Keyboard != null) { action = item.SiNiInputAction; break; }
        if (action?.actionMap == null) return;
        map = action.actionMap;
        system = managers.m_SaveData.m_SystemSaveData;
        baselineKeys = system.m_KeyConfigText ?? "";
        var video = system.m_VideoSetting;
        baselineFullscreen = video.m_IsFullScreen; baselineVsync = video.m_IsVSync; baselineResolution = video.m_ResolutionScale;
        baselineRuntime = input.OutText();
        baseline = ParseBindings(baselineRuntime);
        initialized = true;
        var backup = Path.Combine(PluginData.Root, "native-baseline.json");
        if (!File.Exists(backup))
            PluginData.WriteAtomic(backup, JsonSerializer.Serialize(new { version = 1, capturedUtc = DateTime.UtcNow,
                keyConfigText = baselineKeys, fullscreen = baselineFullscreen, vsync = baselineVsync, resolutionScale = baselineResolution }, jsonOptions));
        var legacy = baseline.Values.Any(value => value.TryGetProperty("path", out var path)
            && (path.GetString() == "" || path.GetString()?.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase) == true));
        if (legacy)
        {
            PluginData.Backup(SystemSaveData.SystemFilePath, "System-before-isolation");
            PluginData.Backup(SystemSaveData.SystemFilePathSpare, "SystemSpare-before-isolation");
            log.LogWarning("Existing mouse/cleared overrides were found in the native profile. They are retained and backed up; bindings overwritten by older versions cannot be reconstructed without an older backup.");
        }
        var merged = new Dictionary<string, JsonElement>(baseline);
        if (File.Exists(PluginData.BindingsPath))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(PluginData.BindingsPath));
                if (document.RootElement.GetProperty("version").GetInt32() != 1) throw new InvalidDataException("Unknown binding profile version.");
                foreach (var change in document.RootElement.GetProperty("changes").EnumerateArray())
                {
                    var id = change.GetProperty("id").GetString() ?? throw new InvalidDataException("Missing binding ID.");
                    if (change.TryGetProperty("remove", out var remove) && remove.GetBoolean()) merged.Remove(id);
                    else merged[id] = change.Clone();
                }
                map.Cast<IInputActionCollection2>().LoadBindingOverridesFromJson(SerializeBindings(merged));
                RefreshActions();
            }
            catch (Exception error) when (error is JsonException or IOException or InvalidDataException or InvalidOperationException or KeyNotFoundException)
            {
                PluginData.Backup(PluginData.BindingsPath, "bindings-invalid-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".json");
                map.Cast<IInputActionCollection2>().LoadBindingOverridesFromJson(baselineRuntime);
                log.LogWarning("Could not apply the plugin binding profile; using the native baseline. " + error.Message);
            }
        }
        lastRuntime = input.OutText();
        if (!File.Exists(PluginData.BindingsPath)) Save(force: true);
        log.LogInfo("Native binding baseline captured; plugin rebinding and video overrides are isolated from native profile saves.");
    }

    public void Save(bool force = false)
    {
        if (!initialized || input == null) return;
        var current = input.OutText();
        if (!force && current == lastRuntime) return;
        var values = ParseBindings(current);
        var changes = new List<JsonElement>();
        foreach (var entry in values)
            if (!baseline.TryGetValue(entry.Key, out var original) || original.GetRawText() != entry.Value.GetRawText()) changes.Add(entry.Value);
        foreach (var id in baseline.Keys.Where(id => !values.ContainsKey(id)))
            changes.Add(JsonSerializer.SerializeToElement(new { id, remove = true }));
        PluginData.WriteAtomic(PluginData.BindingsPath, JsonSerializer.Serialize(new { version = 1, changes }, jsonOptions));
        lastRuntime = current;
    }

    internal SaveState? BeforeSave(SystemSaveData target)
    {
        if (!initialized) return null;
        try { Save(); }
        catch (Exception error) { log.LogError("Could not persist HeatSeekar bindings: " + error); }
        var state = new SaveState(target.m_KeyConfigText, target.m_VideoSetting);
        target.m_KeyConfigText = baselineKeys;
        target.m_VideoSetting = new VideoSetting { m_IsFullScreen = baselineFullscreen, m_IsVSync = baselineVsync, m_ResolutionScale = baselineResolution };
        return state;
    }
    internal static void AfterSave(SystemSaveData target, SaveState? state)
    {
        if (state == null) return;
        target.m_KeyConfigText = state.Keys; target.m_VideoSetting = state.Video;
    }
    private static Dictionary<string, JsonElement> ParseBindings(string json)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return result;
        using var document = JsonDocument.Parse(json);
        foreach (var entry in document.RootElement.GetProperty("bindings").EnumerateArray())
            result.Add(entry.GetProperty("id").GetString()!, entry.Clone());
        return result;
    }
    private static string SerializeBindings(Dictionary<string, JsonElement> values) => JsonSerializer.Serialize(new { bindings = values.Values });
    private void RefreshActions()
    {
        foreach (var item in input!.InputObjectList) item?.UpdateInputAction();
        Plugin.Runtime?.Menu.RefreshBindings();
    }
    public void Dispose()
    {
        if (!initialized) return;
        Save();
        if (map != null) { map.Cast<IInputActionCollection2>().LoadBindingOverridesFromJson(baselineRuntime); RefreshActions(); }
        if (system != null) system.m_KeyConfigText = baselineKeys;
        initialized = false;
    }
}

using HeatSeekar.Core;
using UnityEngine;

namespace HeatSeekar;

// Values and actions for the injected native pages; no independent window.
internal sealed class SettingsModel
{
    internal sealed class Row
    {
        // Name identifies native objects; displayed text comes only from JSON keys.
        public string Name = "", LabelKey = "", HelpKey = "";
        public bool Video, Audio;
        public Func<string> Value = () => "";
        public Action<int>? Change;
        public Action? Activate, Reset;
        public Func<bool>? IsOn;
    }
    private static readonly int[] frameRates = { 0, 30, 60, 90, 120, 144, 165, 240, -1 };
    private readonly DisplayOptions display;
    private readonly Localization text;
    private readonly List<Row> rows = new();
    internal IReadOnlyList<Row> Options => rows;

    public SettingsModel(Settings settings, DisplayOptions display, Localization text)
    {
        this.display = display; this.text = text;
        Toggle("Mouse aiming", "ui_label_mouse_aiming", () => settings.AimEnabled.Value, value => settings.AimEnabled.Value = value, false,
            "ui_help_cheat_warning");
        Toggle("Confine cursor", "ui_label_confine_cursor", () => settings.ConfineCursor.Value, value => settings.ConfineCursor.Value = value, true,
            "ui_help_confine_cursor");
        Add("Cursor size", "ui_label_cursor_size", () => settings.CursorSize.Value.ToString("0") + " px",
            step => settings.CursorSize.Value = Mathf.Clamp(settings.CursorSize.Value + step * 2, 8, 80), () => settings.CursorSize.Value = Settings.DefaultCursorSize,
            "ui_help_cursor_size");
        Toggle("Aki-style menu navigation", "ui_label_navigation", () => settings.MenuNavigation.Value, value => settings.MenuNavigation.Value = value, true,
            "ui_help_navigation");
        Toggle("Exploding Arrow Magic alt trigger", "ui_label_exploding_arrow", () => settings.AltExplosionTrigger.Value, value => settings.AltExplosionTrigger.Value = value, false,
            "ui_help_exploding_arrow");
        Toggle("Hide portrait", "ui_label_hide_portrait", () => settings.HidePortrait.Value, value => settings.HidePortrait.Value = value, false,
            "ui_help_hide_portrait");
        Toggle("Hide HUD", "ui_label_hide_hud", () => settings.HideHud.Value, value => settings.HideHud.Value = value, false,
            "ui_help_hide_hud");
        Toggle("Enable BepInEx console", "ui_label_console", () => settings.EnableConsole.Value, value => settings.EnableConsole.Value = value, false,
            "ui_help_console");
        Toggle("Mute in background", "ui_label_background_audio", () => settings.MuteInBackground.Value, value => settings.MuteInBackground.Value = value, false,
            "ui_help_background_audio", audio: true);
        Toggle("Fullscreen", "ui_label_fullscreen", () => display.Current.Fullscreen, display.SetFullscreen, false,
            "ui_help_fullscreen", true);
        Toggle("V-sync", "ui_label_vsync", () => display.Current.VSync, display.SetVSync, false,
            "ui_help_vsync", true);
        Add("Resolution", "ui_label_resolution", () => $"{display.Current.Surface.Width} x {display.Current.Surface.Height}", ChangeResolution,
            () => { if (!display.Current.Fullscreen) display.SetResolution(Settings.DefaultVideo.Surface); }, "", true);
        Add("Frame rate", "ui_label_frame_rate", () => display.Current.FrameRate == 0 ? text.Text("ui_value_native_fps") : display.Current.FrameRate < 0 ? text.Text("ui_value_unlimited") : display.Current.FrameRate + " FPS",
            step => display.SetFrameRate(Cycle(frameRates, display.Current.FrameRate, step)), () => display.SetFrameRate(Settings.DefaultVideo.FrameRate),
            "ui_help_frame_rate", true);
        Toggle("Preserve 2:1 display ratio", "ui_label_preserve_aspect", () => display.Current.PreserveAspect, display.SetAspect, true,
            "ui_help_preserve_aspect", true);
    }
    internal string Description(Row? row, bool includePrefix = true)
    {
        var value = row?.HelpKey ?? "";
        var recommended = row?.LabelKey == "ui_label_preserve_aspect" ? text.Text("ui_prefix_recommended") + " " : "";
        return value.Length == 0 ? "" : recommended + (includePrefix ? text.Text("ui_prefix_function") + " " : "") + text.Text(value);
    }
    internal void ResetVideoDefaults() => display.ResetDefaults();
    private void Toggle(string name, string labelKey, Func<bool> get, Action<bool> set, bool defaultValue, string helpKey, bool video = false, bool audio = false)
        => rows.Add(new Row { Name = name, LabelKey = labelKey, HelpKey = helpKey, Video = video, Audio = audio, IsOn = get,
            Value = () => text.Text(get() ? "ui_value_on" : "ui_value_off"), Change = _ => set(!get()), Reset = () => set(defaultValue) });
    private void Add(string name, string labelKey, Func<string> value, Action<int> change, Action reset, string helpKey, bool video = false)
        => rows.Add(new Row { Name = name, LabelKey = labelKey, HelpKey = helpKey, Video = video, Value = value, Change = change, Reset = reset });
    private void ChangeResolution(int step)
    {
        if (display.Current.Fullscreen) return;
        var current = display.Current.Surface;
        var options = new HashSet<(int Width, int Height)> { (current.Width, current.Height), (1280, 640), (1440, 720), (1920, 960), (2560, 1280),
            (1280, 720), (1600, 900), (1920, 1080), (2560, 1440) };
        foreach (var resolution in Screen.resolutions) options.Add((resolution.width, resolution.height));
        var sizes = options.Select(size => new DisplaySelection(size.Width, size.Height, FullScreenMode.Windowed)).Where(DisplayOptions.Available)
            .OrderBy(size => size.Width).ThenBy(size => size.Height).ToArray();
        if (sizes.Length > 0) display.SetResolution(Cycle(sizes, current, step));
    }
    private static T Cycle<T>(T[] values, T current, int step) => values[MenuPolicy.Wrap(Array.IndexOf(values, current), step, values.Length)];
}

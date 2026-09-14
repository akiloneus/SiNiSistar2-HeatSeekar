using BepInEx.Configuration;
using UnityEngine;

namespace HeatSeekar;

internal sealed class Settings
{
    internal static VideoSelection DefaultVideo => new(1440, 720, false, false, 240, true);
    internal const float DefaultCursorSize = 24f;
    public ConfigEntry<bool> AimEnabled { get; }
    public ConfigEntry<bool> MenuNavigation { get; }
    public ConfigEntry<bool> ConfineCursor { get; }
    public ConfigEntry<float> CursorSize { get; }
    public ConfigEntry<string> CursorImagePath { get; }
    public ConfigEntry<float> CursorHotspotX { get; }
    public ConfigEntry<float> CursorHotspotY { get; }
    public ConfigEntry<string> CrosshairImagePath { get; }
    public ConfigEntry<float> CrosshairHotspotX { get; }
    public ConfigEntry<float> CrosshairHotspotY { get; }
    public ConfigEntry<float> MinimumAimDistance { get; }
    public ConfigEntry<bool> EnableConsole { get; }
    public ConfigEntry<bool> HidePortrait { get; }
    public ConfigEntry<bool> HideHud { get; }
    public ConfigEntry<bool> MuteInBackground { get; }
    public ConfigEntry<bool> AltExplosionTrigger { get; }
    public ConfigEntry<int> FrameRate { get; }
    public ConfigEntry<bool> DisplayInitialized { get; }
    public ConfigEntry<bool> VSync { get; }
    public ConfigEntry<int> DisplayWidth { get; }
    public ConfigEntry<int> DisplayHeight { get; }
    public ConfigEntry<FullScreenMode> DisplayMode { get; }
    public ConfigEntry<bool> PreserveAspect { get; }
    private readonly ConfigFile config;

    public Settings(ConfigFile config)
    {
        this.config = config;
        AimEnabled = config.Bind("Aim", "Enabled", false, "(Not recommended) CHEAT: Enabling this feature will alter the game's original design and intended experience.");
        MenuNavigation = config.Bind("Menu", "Aki-style Navigation", true, "Enable pointer/focus switching, wheel navigation, hidden-pointer click confirmation and Q/R binding shortcuts.");
        ConfineCursor = config.Bind("Cursor", "Confine To Window", true, "Confine the pointer during gameplay.");
        CursorSize = config.Bind("Cursor", "Size", DefaultCursorSize, new ConfigDescription("Longest image edge for both pointer and crosshair at 1080p, in pixels. The image keeps its aspect ratio.", new AcceptableValueRange<float>(8f, 80f)));
        const string imageHelp = "PNG path relative to BepInEx/plugins/HeatSeekar. Missing or invalid images use the built-in default. PNG edits reload within one second while shown; restart after editing this configuration.";
        const string hotspotHelp = "Image hotspot as a fraction from 0 to 1, measured from the top-left corner; 0.5 is the center. Restart after editing this configuration.";
        CursorImagePath = config.Bind("Cursor", "ImagePath", "assets/cursor.png", imageHelp);
        CursorHotspotX = config.Bind("Cursor", "HotspotX", 0.5f, new ConfigDescription(hotspotHelp, new AcceptableValueRange<float>(0f, 1f)));
        CursorHotspotY = config.Bind("Cursor", "HotspotY", 0.5f, new ConfigDescription(hotspotHelp, new AcceptableValueRange<float>(0f, 1f)));
        CrosshairImagePath = config.Bind("Crosshair", "ImagePath", "assets/crosshair.png", imageHelp);
        CrosshairHotspotX = config.Bind("Crosshair", "HotspotX", 0.5f, new ConfigDescription(hotspotHelp, new AcceptableValueRange<float>(0f, 1f)));
        CrosshairHotspotY = config.Bind("Crosshair", "HotspotY", 0.5f, new ConfigDescription(hotspotHelp, new AcceptableValueRange<float>(0f, 1f)));
        MinimumAimDistance = config.Bind("Aim", "Minimum Distance", 0.05f, new ConfigDescription("Shoot straight ahead when the cursor is too close to the launch point.", new AcceptableValueRange<float>(0.001f, 1f)));
        EnableConsole = config.Bind("Diagnostics", "Enable BepInEx Console", false, "Show BepInEx's console for log output.");
        HidePortrait = config.Bind("Interface", "Hide Portrait", false, "Hide the character portrait and move the HP/MP gauge to the lower left, with Name and Relics to its right.");
        HideHud = config.Bind("Interface", "Hide HUD", false, "Hide the HP/MP gauges, status effects, and Name and Relics tags, including their labels and values.");
        MuteInBackground = config.Bind("Audio", "Mute In Background", false, "Mute game audio while the game window is not focused.");
        AltExplosionTrigger = config.Bind("Combat", "Exploding Arrow Alt Trigger", false, "While aiming Shoot Magic, press Attack to trigger Exploding Arrow Magic. The original combination remains available.");
        FrameRate = config.Bind("Display", "Frame Rate", DefaultVideo.FrameRate, new ConfigDescription("0 uses SiNi original frame pacing; -1 is uncapped; a positive value sets an FPS cap and disables V-sync. Video changes apply and save immediately.", new AcceptableValueRange<int>(-1, 1000)));
        DisplayInitialized = config.Bind("Display", "Initialized", false, "Plugin video defaults are saved on first launch; saved settings are restored on later launches.");
        VSync = config.Bind("Display", "V-sync", false, "Saved V-sync setting.");
        DisplayWidth = config.Bind("Display", "Width", DefaultVideo.WindowWidth, new ConfigDescription("Saved window width, preserved independently of fullscreen.", new AcceptableValueRange<int>(640, 16384)));
        DisplayHeight = config.Bind("Display", "Height", DefaultVideo.WindowHeight, new ConfigDescription("Saved window height, preserved independently of fullscreen.", new AcceptableValueRange<int>(360, 8640)));
        DisplayMode = config.Bind("Display", "Mode", FullScreenMode.Windowed, "Saved display mode.");
        PreserveAspect = config.Bind("Display", "Preserve 2:1 Display Ratio", DefaultVideo.PreserveAspect, "Render the game in a centered 2:1 viewport at the window's native pixel size, with black bars outside it.");
    }
    internal void SaveVideo(VideoSelection value)
    {
        config.SaveOnConfigSet = false;
        try
        {
            DisplayWidth.Value = value.WindowWidth; DisplayHeight.Value = value.WindowHeight;
            DisplayMode.Value = value.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            VSync.Value = value.VSync; FrameRate.Value = value.FrameRate; PreserveAspect.Value = value.PreserveAspect;
            DisplayInitialized.Value = true; config.Save();
        }
        finally { config.SaveOnConfigSet = true; }
    }
}

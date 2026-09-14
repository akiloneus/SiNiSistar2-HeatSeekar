namespace HeatSeekar;

// Frozen compatibility aliases for external JSON shipped before semantic keys.
internal static class LocalizationKeys
{
    private static readonly Dictionary<string, string> Legacy = new(StringComparer.Ordinal)
    {
        ["(Not recommended) CHEAT: Enabling this feature will alter the game's original design and intended experience."] = "ui_help_cheat_warning",
        ["Aki-style menu navigation"] = "ui_label_navigation",
        ["Back"] = "ui_label_back",
        ["Confine cursor"] = "ui_label_confine_cursor",
        ["Cursor size"] = "ui_label_cursor_size",
        ["Diameter at 1080p. The sprite scales with the window height."] = "ui_help_cursor_size",
        ["Enable BepInEx console"] = "ui_label_console",
        ["Exploding Arrow Magic alt trigger"] = "ui_label_exploding_arrow",
        ["Frame rate"] = "ui_label_frame_rate",
        ["Fullscreen"] = "ui_label_fullscreen",
        ["HeatSeekar"] = "ui_label_heatseekar",
        ["HeatSeekar function:"] = "ui_prefix_function",
        ["Hide HUD"] = "ui_label_hide_hud",
        ["Hide portrait"] = "ui_label_hide_portrait",
        ["Hide the HP/MP gauges, status effects, and Name and Relics tags, including their labels and values."] = "ui_help_hide_hud",
        ["Hide the character portrait and move the HP/MP gauge to the lower left, with Name and Relics to its right."] = "ui_help_hide_portrait",
        ["Hybrid menu navigation with mouse, keyboard, and gamepad support and additional key bindings.\nInspired by Akiloneus's doujin game project—Rabiane."] = "ui_help_navigation",
        ["Keep the pointer inside the game window during gameplay."] = "ui_help_confine_cursor",
        ["Mouse aiming"] = "ui_label_mouse_aiming",
        ["Mute game audio while the game window is not focused."] = "ui_help_background_audio",
        ["Mute in background"] = "ui_label_background_audio",
        ["Off"] = "ui_value_off",
        ["On"] = "ui_value_on",
        ["Preserve 2:1 display ratio"] = "ui_label_preserve_aspect",
        ["Q: clear a binding. R: restore its default. These shortcuts apply outside key capture."] = "ui_help_bindings",
        ["Recommended. Preserve the game's original 2:1 aspect ratio at any resolution. Unused space is black."] = "ui_help_preserve_aspect",
        ["Resolution"] = "ui_label_resolution",
        ["Return to the game menu. Settings are saved automatically."] = "ui_help_back",
        ["Show BepInEx's console for log output."] = "ui_help_console",
        ["SiNi original"] = "ui_value_native_fps",
        ["SiNi original uses the game's frame pacing. A frame cap or Unlimited disables V-sync."] = "ui_help_frame_rate",
        ["Synchronize frames with the display. Enabling V-sync also enables Fullscreen and SiNi original frame pacing."] = "ui_help_vsync",
        ["Unlimited"] = "ui_value_unlimited",
        ["Use a borderless window at the desktop resolution. Turning this off restores the window size and disables V-sync."] = "ui_help_fullscreen",
        ["V-sync"] = "ui_label_vsync",
        ["While aiming Shoot Magic, press Attack to trigger Exploding Arrow Magic. The original combination remains available."] = "ui_help_exploding_arrow",
    };

    internal static string Resolve(string key) => Legacy.TryGetValue(key, out var canonical) ? canonical : key;
}

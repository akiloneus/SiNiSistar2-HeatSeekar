# SiNiSistar2 HeatSeekar

HeatSeekar is a free quality-of-life mod for SiNiSistar2 with native mouse bindings, mouse support for menus, and improved display options. Settings are integrated into the game's native menus.

[Project page](https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/) · [Download](#download) · [Report an issue](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/issues)

## Download

Get the [**latest HeatSeekar release**](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest) for **Windows x64**. Tested with SiNiSistar2 **1.2.1** and **1.3.1**.

Open **Assets** on the release page. Choose a ZIP ending in `-Full.zip` or `-ModOnly.zip`:

| Package | When to use |
| --- | --- |
| **Full** — Mod + BepInEx | First installation in a game folder without BepInEx. |
| **ModOnly** — Mod files only | Updating HeatSeekar, or installing it alongside a compatible BepInEx 6 IL2CPP setup. |

Both packages include default translation JSON files and cursor/crosshair PNGs. Full also includes BepInEx for **Unity IL2CPP / Windows x64**. GitHub's automatic **Source code** downloads are for viewing or building the source, not installing the mod. Version details and changes are in the release notes.

[All releases](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases) · [Source repository](https://github.com/akiloneus/SiNiSistar2-HeatSeekar)

## How to install

1. Close the game.
2. Extract the ZIP selected above directly into the folder containing `SiNiSistar2.exe`. Merge folders and replace files when prompted. Do not put the extracted files inside an extra package-named folder.
3. Start the game and configure HeatSeekar through the native Settings and Graphics pages. The first launch with BepInEx may take longer while it prepares its files.

Both packages place the mod files here:

```text
Game folder/
├── SiNiSistar2.exe
└── BepInEx/
    └── plugins/
        └── HeatSeekar/
            ├── SiNiSistar2.HeatSeekar.dll
            ├── assets/
            └── localization/
```

The Full package also installs the BepInEx runtime files in the game folder.

## Updating

1. Download **ModOnly** (`*-ModOnly.zip`) from the [latest release](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest). Check its release notes for any BepInEx requirement changes.
2. Close the game.
3. Extract the ZIP into the same folder containing `SiNiSistar2.exe`, merging folders and replacing files when prompted.

**Keeping custom files:** Extracting the whole package can overwrite translation JSON files and cursor/crosshair PNGs under `localization/` and `assets/`. Back up your edits first. To keep those files unchanged, extract only `SiNiSistar2.HeatSeekar.dll` from the ModOnly ZIP and replace the existing file at `BepInEx/plugins/HeatSeekar/SiNiSistar2.HeatSeekar.dll`. This requires an already compatible BepInEx installation.

## Features

- **Native mouse support:** Bind mouse buttons directly to in-game actions and navigate menus with the mouse.
- **Gallery controls:** With Aki menu navigation enabled, click the on-screen hints to use their actions. Keycaps darken slightly on hover; Q/E change characters, L toggles slow motion, and P toggles pause.
- **Display options:** Adjust resolution, borderless fullscreen, V-sync, and frame-rate limits, with support for preserving the original 2:1 aspect ratio.
- **Mute in background:** Mute game audio while the game window is not focused.
- **Audio menu fix:** Keep volume values visible at higher resolutions.
- **Exploding Arrow Magic alt trigger:** Press Attack while aiming Shoot Magic to trigger Exploding Arrow Magic. The original combination remains available.
- **Mouse aiming:** Control attack direction and Shoot Magic aiming with the mouse. This is a cheat feature, not recommended and disabled by default.
- **Customization:** Use your own cursor/crosshair PNGs and edit translation JSON files, including translations for custom game languages.

## Customization

HeatSeekar stores its files under `BepInEx/plugins/HeatSeekar/`.

- **Translations:** Edit `localization/<language>/ui.json`. Changes reload while the game is running. English, Japanese, Korean, Simplified Chinese, and Traditional Chinese are included. HeatSeekar also creates matching folders for custom languages registered by the game; languages without a built-in translation start with English.
- **Cursor and crosshair:** Replace `assets/cursor.png` and `assets/crosshair.png`, or set `ImagePath` in the `[Cursor]` and `[Crosshair]` sections of `HeatSeekar.cfg`. `HotspotX` and `HotspotY` range from 0 to 1, measured from the image's top-left corner; `0.5, 0.5` is the center. Restart the game after changing these configuration settings.

Missing default translation and image files are generated from the DLL's embedded resources. Existing files are preserved when the mod starts. Installing an update package can still replace them.

## Feedback

If you encounter a problem, [open an issue](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/issues) with your game version, steps to reproduce the problem, and `BepInEx/LogOutput.log`.

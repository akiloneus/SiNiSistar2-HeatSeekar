<p align="center">
    <img src="https://akiloneus.github.io/rabiane_pray_loop_title.gif" alt="Rabiane">
</p>

<h1 align="center">SiNiSistar2 HeatSeekar</h1>

<p align="left">
  <a href="https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest"><img src="https://img.shields.io/github/v/release/akiloneus/SiNiSistar2-HeatSeekar?label=Release&logo=github&color=blue" alt="Latest Release"></a>
  <a href="https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases"><img src="https://img.shields.io/github/downloads/akiloneus/SiNiSistar2-HeatSeekar/total?label=Downloads&color=success" alt="Downloads"></a>
  <a href="https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest"><img src="https://img.shields.io/badge/Platform-Windows%20x64-lightgrey?logo=windows" alt="Platform"></a>
</p>

HeatSeekar is a free quality-of-life mod for SiNiSistar2 with native mouse bindings, mouse support for menus, and improved display options. Settings are integrated into the game's native menus.

Tested with SiNiSistar2 **1.2.1** and **1.3.1**; the mod is also compatible with the lite version and Steam version of SiNiSistar2.

[Project page](https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/) · [Download](#download) · [Report an issue](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/issues)

## Preview
<p align="center">
  <a href="https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/">
    <img src="https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/media/screenshot_menu_preview.jpg" alt="Menu Preview">
  </a>
</p>


## Download

Get the [**latest HeatSeekar release**](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest) for **Windows x64**. 

Open **Assets** on the release page. Choose a ZIP ending in `-Full.zip` or `-ModOnly.zip`:

| Package | When to use |
| --- | --- |
| **Full** — Mod + BepInEx | First installation if you haven't installed BepInEx for the game before. |
| **ModOnly** — Mod files only | Updating HeatSeekar, or if you have installed a compatible BepInEx 6 IL2CPP for the game. |

Both packages include default translation JSON files and cursor/crosshair PNGs. Full also includes BepInEx for **Unity IL2CPP / Windows x64**. GitHub's automatic **Source code** downloads are for viewing or building the source, not installing the mod. Version details and changes are in the release notes.

[All releases](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases) · [Source repository](https://github.com/akiloneus/SiNiSistar2-HeatSeekar)

## Install

1. Extract the ZIP selected above directly into the folder containing `SiNiSistar2.exe`. Merge folders and replace files when prompted. Do not put the extracted files inside an extra package-named folder.
2. Start the game and configure HeatSeekar through the native Settings and Graphics pages. The first launch with BepInEx may take longer while it prepares its files.

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

## Update

1. Download **ModOnly** (`*-ModOnly.zip`) from the [latest release](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/latest). Check its release notes for any BepInEx requirement changes.
2. Extract the ZIP into the same folder containing `SiNiSistar2.exe`, merging folders and replacing files when prompted.

**Keeping custom files:** Extracting the whole package can overwrite translation JSON files and cursor/crosshair PNGs under `localization/` and `assets/`. Back up your edits first. To keep those files unchanged, extract only `SiNiSistar2.HeatSeekar.dll` from the ModOnly ZIP and replace the existing file at `BepInEx/plugins/HeatSeekar/SiNiSistar2.HeatSeekar.dll`. This requires an already compatible BepInEx installation.

## Features

> More QoL functions may be added in the future. You can preview features with screenshots and video clips on the [Project page](https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/#features).

### Graphics
- **Improved resolution:** Allows you to set a resolution that fits your monitor better while preserving the game's original 2:1 aspect ratio.
- **Improved fullscreen mode:** A better implementation of fullscreen display mode that keeps UI elements sharp and clear (no more blurry upscaling).
- **Improved framerate limiter:** Allows you to set a framerate cap or unlock it completely.

### Audio
- **Mute in background toggle:** Mute game audio while the window is not focused. (Don't let others hear Sistars' moaning in the background when you alt-tab out!)

### UI/UX
- **Native mouse support:** Bind mouse buttons directly to in-game actions and navigate menus with the mouse. (Yes, you can now navigate the gallery with a single hand, using a keyboard or mouse.)
- **Gallery controls:** With Aki menu navigation enabled, click the on-screen hints to use their actions. Keycaps darken slightly on hover; Q/E change characters, L toggles slow motion, and P toggles pause.
- **Hide portrait and/or HUD:** HUD layout changes to compact mode when the portrait is hidden for a clearer view.
- **Audio menu fix:** Keeps volume values visible at higher resolutions.

### Gameplay
- **Exploding Arrow alt trigger:** An alternate key combination for triggering Exploding Arrow Magic (Shoot Magic aiming + Attack). The original combination remains available.
- **Character mouse aiming:** A cheat feature (not recommended, disabled by default) allows you to control attack direction and Shoot Magic aiming with the mouse.

## Customization

HeatSeekar stores its files under `BepInEx/plugins/HeatSeekar/`.

- **Translations:** Edit `localization/<language>/ui.json`. Changes reload while the game is running. English, Japanese, Korean, Simplified Chinese, and Traditional Chinese are included. HeatSeekar also creates matching folders for custom languages registered by the game; languages without a built-in translation start with English.
- **Cursor and crosshair:** Replace `assets/cursor.png` and `assets/crosshair.png`, or set `ImagePath` in the `[Cursor]` and `[Crosshair]` sections of `HeatSeekar.cfg`. `HotspotX` and `HotspotY` range from 0 to 1, measured from the image's top-left corner; `0.5, 0.5` is the center. Restart the game after changing these configuration settings.

Missing default translation and image files are generated from the DLL's embedded resources. Existing files are preserved when the mod starts. Installing an update package can still replace them.

## Feedback

If you encounter a problem, [open an issue](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/issues) with your game version, steps to reproduce the problem, and `BepInEx/LogOutput.log`.

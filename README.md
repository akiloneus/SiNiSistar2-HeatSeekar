# SiNiSistar2 HeatSeekar

HeatSeekar is a free quality-of-life mod for SiNiSistar2 with native mouse bindings, mouse support for menus, and improved display options. Settings are integrated into the game's native menus.

[Project page](https://akiloneus.github.io/resources/sinisistar2-heatseekar-mod/) · [Download](#download) · [Report an issue](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/issues)

## Download

Current release: [**HeatSeekar 0.8.0**](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/tag/v0.8.0) for **Windows x64**. Tested with SiNiSistar2 **1.2.1** and **1.3.1**.

| Package | Choose this if… |
| --- | --- |
| [**Full package — includes BepInEx**](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/download/v0.8.0/SiNiSistar2.HeatSeekar-0.8.0-win-x64-Full.zip) | You are installing HeatSeekar for the first time and need BepInEx. |
| [**Mod only / Update**](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases/download/v0.8.0/SiNiSistar2.HeatSeekar-0.8.0-win-x64-ModOnly.zip) | You already have a compatible BepInEx 6 IL2CPP installation. |

The full package includes BepInEx **6.0.0-be.754** for Unity IL2CPP / Windows x64.

Use one of the two packages above to install the mod. GitHub's automatic **Source code** downloads contain the source files.

[All releases](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/releases) · [Source repository](https://github.com/akiloneus/SiNiSistar2-HeatSeekar)

## Features

- **Native mouse support:** Bind mouse buttons directly to in-game actions and navigate menus with the mouse.
- **Display options:** Adjust resolution, borderless fullscreen, V-sync, and frame-rate limits, with support for preserving the original 2:1 aspect ratio.
- **Mute in background:** Mute game audio while the game window is not focused.
- **Audio menu fix:** Keep volume values visible at higher resolutions.
- **Exploding Arrow Magic alt trigger:** Press Attack while aiming Shoot Magic to trigger Exploding Arrow Magic. The original combination remains available.
- **Mouse aiming:** Control attack direction and Shoot Magic aiming with the mouse. This is a cheat feature, not recommended and disabled by default.
- **Customization:** Use your own cursor/crosshair PNGs and edit translation JSON files, including translations for custom game languages.

## How to install

Close the game, then extract the package into the folder containing `SiNiSistar2.exe`, merging folders and replacing files when prompted. After installing the full package, the relevant files should look like this:

```text
Game folder/
├── SiNiSistar2.exe                # Existing game file
├── SiNiSistar2_Data/              # Existing game files
├── BepInEx/
│   ├── core/                      # BepInEx
│   └── plugins/
│       └── HeatSeekar/            # HeatSeekar (both packages)
│           ├── SiNiSistar2.HeatSeekar.dll
│           ├── assets/            # Cursor and crosshair PNGs
│           └── localization/      # Translation JSON files
├── dotnet/                        # BepInEx
├── .doorstop_version              # BepInEx
├── changelog.txt                  # BepInEx
├── doorstop_config.ini            # BepInEx
└── winhttp.dll                    # BepInEx
```

The mod-only package uses the same `BepInEx/plugins/HeatSeekar/` location and requires a compatible BepInEx 6 IL2CPP installation.

Start the game and configure HeatSeekar through the native Settings and Graphics pages. The first launch with BepInEx may take longer while it prepares its files.

## Updating

Close the game and extract the new package into the same game folder, replacing files when prompted.

Updates include the default translation JSON files and cursor/crosshair PNGs. Back up any customized files before replacing them. If you want to retain your custom files, you can update only `BepInEx/plugins/HeatSeekar/SiNiSistar2.HeatSeekar.dll` when your BepInEx installation is already compatible.

## Customization

HeatSeekar stores its files under `BepInEx/plugins/HeatSeekar/`.

- **Translations:** Edit `localization/<language>/ui.json`. Changes reload while the game is running. English, Japanese, Korean, Simplified Chinese, and Traditional Chinese are included. HeatSeekar also creates matching folders for custom languages registered by the game; languages without a built-in translation start with English.
- **Cursor and crosshair:** Replace `assets/cursor.png` and `assets/crosshair.png`, or set `ImagePath` in the `[Cursor]` and `[Crosshair]` sections of `HeatSeekar.cfg`. `HotspotX` and `HotspotY` range from 0 to 1, measured from the image's top-left corner; `0.5, 0.5` is the center. Restart the game after changing these configuration settings.

Missing default translation and image files are generated from the DLL's embedded resources. Existing files are preserved when the mod starts. Installing an update package can still replace them.

## Feedback

If you encounter a problem, [open an issue](https://github.com/akiloneus/SiNiSistar2-HeatSeekar/issues) with your game version, steps to reproduce the problem, and `BepInEx/LogOutput.log`.

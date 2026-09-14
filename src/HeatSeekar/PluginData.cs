using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace HeatSeekar;

internal static class PluginData
{
    internal static string Root => Path.Combine(Paths.PluginPath, "HeatSeekar");
    internal static string BindingsPath => Path.Combine(Root, "bindings.json");

    internal static Settings OpenSettings(ManualLogSource log)
    {
        Directory.CreateDirectory(Root);
        var configPath = Path.Combine(Root, "HeatSeekar.cfg");
        var legacy = Path.Combine(Paths.ConfigPath, Plugin.Id + ".cfg");
        if (!File.Exists(configPath) && File.Exists(legacy))
        {
            Backup(legacy, "legacy-config.cfg");
            var lines = new List<string>();
            foreach (var original in File.ReadAllLines(legacy))
            {
                var line = original.Trim();
                if (line.StartsWith("#") || line.StartsWith("Settings Hotkey", StringComparison.Ordinal)) continue;
                line = line.Replace("Rabiane Navigation", "Aki-style Navigation").Replace("Restore On Launch", "Initialized")
                    .Replace("ExclusiveFullScreen", "FullScreenWindow");
                lines.Add(line);
            }
            WriteAtomic(configPath, string.Join(Environment.NewLine, lines));
            log.LogInfo("Imported legacy HeatSeekar configuration; the original is backed up under plugins/HeatSeekar/migration-backup.");
        }
        var oldTranslations = Path.Combine(Paths.BepInExRootPath, "heatseekar", "localization");
        if (Directory.Exists(oldTranslations))
            foreach (var directory in Directory.EnumerateDirectories(oldTranslations))
            {
                var source = Path.Combine(directory, "ui.json");
                var destination = Path.Combine(Root, "localization", Path.GetFileName(directory), "ui.json");
                if (!File.Exists(source) || File.Exists(destination)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, false);
            }
        return new Settings(new ConfigFile(configPath, true));
    }

    internal static void Backup(string source, string name)
    {
        if (!File.Exists(source)) return;
        var directory = Path.Combine(Root, "migration-backup");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, name);
        if (!File.Exists(destination)) File.Copy(source, destination, false);
    }

    internal static void WriteAtomic(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, content, new UTF8Encoding(false));
        File.Move(temporary, path, true);
    }
}

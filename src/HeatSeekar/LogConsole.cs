using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;

namespace HeatSeekar;

internal sealed class LogConsole : IDisposable
{
    private readonly Settings settings;
    private readonly ManualLogSource log;
    private readonly bool originallyVisible;
    private bool? applied;
    private bool created;
    private ConsoleLogListener? listener;

    public LogConsole(Settings settings, ManualLogSource log)
    {
        this.settings = settings; this.log = log;
        originallyVisible = ConsoleManager.ConsoleActive && IsWindowVisible(GetConsoleWindow());
    }

    public void Tick()
    {
        var enabled = settings.EnableConsole.Value;
        if (applied == enabled) return;
        applied = enabled;
        try
        {
            if (enabled)
            {
                if (!ConsoleManager.ConsoleActive) { ConsoleManager.CreateConsole(); created = true; }
                if (!Logger.Listeners.OfType<ConsoleLogListener>().Any())
                { listener = new ConsoleLogListener(); Logger.Listeners.Add(listener); }
                ShowWindow(GetConsoleWindow(), 8);
                log.LogInfo("HeatSeekar console enabled. Arrow target logging is always active.");
            }
            else if (ConsoleManager.ConsoleActive) ShowWindow(GetConsoleWindow(), 0);
        }
        catch (Exception error)
        {
            settings.EnableConsole.Value = false;
            applied = false;
            log.LogError("Could not open the BepInEx console: " + error);
        }
    }

    public void Dispose()
    {
        if (listener != null) { Logger.Listeners.Remove(listener); listener.Dispose(); listener = null; }
        if (created) ConsoleManager.DetachConsole();
        else if (ConsoleManager.ConsoleActive) ShowWindow(GetConsoleWindow(), originallyVisible ? 8 : 0);
    }

    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
}

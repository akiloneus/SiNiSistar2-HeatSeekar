using BepInEx.Logging;
using SiNiSistar2.Manager;
using UnityEngine;

namespace HeatSeekar;

internal readonly record struct DisplaySelection(int Width, int Height, FullScreenMode Mode)
{
    public static DisplaySelection Current => new(Screen.width, Screen.height, Screen.fullScreenMode);
}
internal readonly record struct VideoSelection(int WindowWidth, int WindowHeight, bool Fullscreen, bool VSync, int FrameRate, bool PreserveAspect)
{
    public DisplaySelection Surface => Fullscreen
        ? new(DisplayOptions.DesktopSize.x, DisplayOptions.DesktopSize.y, FullScreenMode.FullScreenWindow)
        : new(WindowWidth, WindowHeight, FullScreenMode.Windowed);
}
internal sealed class DisplayOptions : IDisposable
{
    private readonly Settings settings;
    private readonly ManualLogSource log;
    private DisplaySelection? pending;
    private float pendingBefore;
    private bool startupApplied, applying;
    private int originalFrameRate, originalVsync;
    public VideoSelection Current { get; private set; }
    public bool IsChanging => pending.HasValue;
    public DisplayOptions(Settings settings, ManualLogSource log)
    {
        this.settings = settings; this.log = log;
        Current = new(settings.DisplayWidth.Value, settings.DisplayHeight.Value, settings.DisplayMode.Value != FullScreenMode.Windowed,
            settings.VSync.Value, settings.FrameRate.Value, settings.PreserveAspect.Value);
    }
    public void Tick()
    {
        if (!startupApplied)
        {
            if (GameContext.Managers == null || !ManagerList.HasDoneSceneSetUp) return;
            startupApplied = true;
            originalFrameRate = Application.targetFrameRate; originalVsync = QualitySettings.vSyncCount;
            var saved = Current;
            Current = Coherent(Current);
            if (!Available(Current.Surface))
                Current = Current with { Fullscreen = false, WindowWidth = Math.Min(1280, DesktopSize.x), WindowHeight = Math.Min(640, DesktopSize.y) };
            Apply(Current);
            if (!settings.DisplayInitialized.Value || Current != saved) SaveCurrent();
        }
        if (pending is { } target)
        {
            if (DisplaySelection.Current == target) pending = null;
            else if (Time.realtimeSinceStartup >= pendingBefore)
            {
                pending = null;
                // The OS may reject or adjust a window request. Retain the
                // actual display state instead of restoring an earlier edit.
                var actual = DisplaySelection.Current;
                Current = Coherent(Current with { Fullscreen = actual.Mode != FullScreenMode.Windowed,
                    WindowWidth = actual.Mode == FullScreenMode.Windowed ? actual.Width : Current.WindowWidth,
                    WindowHeight = actual.Mode == FullScreenMode.Windowed ? actual.Height : Current.WindowHeight });
                SaveCurrent();
                log.LogWarning("The requested display size did not settle; saved the actual display state.");
            }
        }
        if (!IsChanging && !Current.Fullscreen && !Screen.fullScreen && Screen.width >= 640 && Screen.height >= 360)
            Current = Current with { WindowWidth = Screen.width, WindowHeight = Screen.height };
        ApplyFramePacing();
    }
    public void SetFullscreen(bool value) => Change(Current with { Fullscreen = value, VSync = value && Current.VSync });
    public void SetVSync(bool value) => Change(Current with { VSync = value, Fullscreen = value || Current.Fullscreen, FrameRate = value ? 0 : Current.FrameRate });
    public void SetFrameRate(int value) => Change(Current with { FrameRate = value, VSync = value == 0 && Current.VSync });
    public void SetAspect(bool value) => Change(Current with { PreserveAspect = value });
    public void SetResolution(DisplaySelection selection)
    {
        selection = Normalize(selection);
        Change(Current with { Fullscreen = selection.Mode != FullScreenMode.Windowed,
            WindowWidth = selection.Mode == FullScreenMode.Windowed ? selection.Width : Current.WindowWidth,
            WindowHeight = selection.Mode == FullScreenMode.Windowed ? selection.Height : Current.WindowHeight,
            VSync = selection.Mode != FullScreenMode.Windowed && Current.VSync });
    }
    private void Change(VideoSelection selection)
    {
        selection = Coherent(selection);
        if (!Available(selection.Surface)) throw new ArgumentOutOfRangeException(nameof(selection), "This display size is not available on the current monitor.");
        Apply(selection);
        SaveCurrent();
    }
    internal void SaveCurrent() => settings.SaveVideo(Current);
    internal DisplaySelection Normalize(DisplaySelection selection) => selection.Mode == FullScreenMode.Windowed
        ? selection : new(DesktopSize.x, DesktopSize.y, FullScreenMode.FullScreenWindow);
    private static VideoSelection Coherent(VideoSelection selection) => selection with { VSync = selection.Fullscreen && selection.VSync && selection.FrameRate == 0 };
    internal void ResetDefaults() => Change(Settings.DefaultVideo);
    internal static Vector2Int DesktopSize
    {
        get
        {
            var monitor = Display.main;
            return monitor != null && monitor.systemWidth > 0 && monitor.systemHeight > 0
                ? new(monitor.systemWidth, monitor.systemHeight) : new(Screen.currentResolution.width, Screen.currentResolution.height);
        }
    }
    internal bool UpdateNativeVideo(SiNiSistar2.UI.Pause.VideoUI page)
    {
        if (!startupApplied) return false;
        if (page.m_ResolutionText != null) page.m_ResolutionText.text = $"{Current.Surface.Width} x {Current.Surface.Height}";
        if (!applying) ApplyFramePacing();
        return true;
    }
    internal static bool Available(DisplaySelection selection)
    {
        if (selection.Width < 640 || selection.Height < 360) return false;
        if (selection.Mode == FullScreenMode.FullScreenWindow) return selection.Width == DesktopSize.x && selection.Height == DesktopSize.y;
        return selection.Mode == FullScreenMode.Windowed && selection.Width <= DesktopSize.x && selection.Height <= DesktopSize.y;
    }
    private void Apply(VideoSelection selection)
    {
        Current = selection; applying = true;
        try
        {
            var save = GameContext.Managers?.m_SaveData;
            if (save != null)
            {
                if (save.IsFullScreen != selection.Fullscreen) save.IsFullScreen = selection.Fullscreen;
                if (save.IsVSync != selection.VSync) save.IsVSync = selection.VSync;
            }
            var surface = selection.Surface;
            var wasPending = pending.HasValue;
            pending = null;
            if (DisplaySelection.Current != surface || wasPending)
            {
                pending = surface; pendingBefore = Time.realtimeSinceStartup + 3f;
                Screen.SetResolution(surface.Width, surface.Height, surface.Mode);
            }
            ApplyFramePacing();
        }
        finally { applying = false; }
    }
    private void ApplyFramePacing()
    {
        QualitySettings.vSyncCount = Current.VSync && Current.Fullscreen && Current.FrameRate == 0 ? 1 : 0;
        Application.targetFrameRate = Current.FrameRate == 0 ? (Current.VSync ? -1 : 60) : Current.FrameRate;
    }
    public void Dispose()
    {
        if (startupApplied) { Application.targetFrameRate = originalFrameRate; QualitySettings.vSyncCount = originalVsync; }
    }
}

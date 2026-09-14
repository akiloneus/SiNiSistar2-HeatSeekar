using UnityEngine;
using UnityEngine.UI;

namespace HeatSeekar;

internal sealed class InterfaceOptions : IDisposable
{
    private readonly Settings settings;
    private readonly VisualMask portrait = new(), hud = new();
    private readonly PortraitHiddenLayout portraitLayout = new();
    private float? unmutedVolume;

    public InterfaceOptions(Settings settings) => this.settings = settings;

    public void Tick()
    {
        var main = GameContext.Managers?.m_UIList?.MainUI;
        portraitLayout.Apply(main, settings.HidePortrait.Value);
        portrait.Apply(settings.HidePortrait.Value && main != null ? new[] { LiveTransform(main!.Portrait) } : Array.Empty<Transform?>());
        hud.Apply(settings.HideHud.Value && main != null
            ? new[] { LiveTransform(main.NameUI), LiveTransform(main.RelicsUI), LiveTransform(main.NameUI)?.parent.Find("Status") } : Array.Empty<Transform?>());
        FocusChanged(Application.isFocused);
    }

    private static Transform? LiveTransform(Component? component) => component != null ? component.transform : null;

    internal void FocusChanged(bool focused)
    {
        if (settings.MuteInBackground.Value && !focused)
        {
            unmutedVolume ??= AudioListener.volume;
            AudioListener.volume = 0;
        }
        else RestoreAudio();
    }

    private void RestoreAudio()
    {
        if (unmutedVolume is not { } volume) return;
        AudioListener.volume = volume;
        unmutedVolume = null;
    }

    public void Dispose() { portraitLayout.Dispose(); portrait.Dispose(); hud.Dispose(); RestoreAudio(); }
}

// Leave native CanvasGroups and GameObjects alone: they also drive transitions.
// Mask only drawing components, preserving their individual enabled states.
internal sealed class VisualMask : IDisposable
{
    private readonly Dictionary<Graphic, bool> graphics = new();
    private readonly Dictionary<Renderer, bool> renderers = new();
    private IntPtr[] roots = Array.Empty<IntPtr>();
    private float nextScan;

    internal void Apply(IEnumerable<Transform?> targets)
    {
        var current = targets.Where(target => target != null).Select(target => target!).ToArray();
        var pointers = current.Select(target => target.Pointer).ToArray();
        if (!roots.SequenceEqual(pointers)) { Dispose(); roots = pointers; nextScan = 0; }
        if (Time.realtimeSinceStartup >= nextScan)
        {
            nextScan = Time.realtimeSinceStartup + 0.25f;
            foreach (var target in current)
            {
                foreach (var graphic in target.GetComponentsInChildren<Graphic>(true))
                    if (!graphics.ContainsKey(graphic)) graphics.Add(graphic, graphic.enabled);
                foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
                    if (!renderers.ContainsKey(renderer)) renderers.Add(renderer, renderer.forceRenderingOff);
            }
        }
        foreach (var graphic in graphics.Keys) if (graphic != null && graphic.enabled) graphic.enabled = false;
        foreach (var renderer in renderers.Keys) if (renderer != null) renderer.forceRenderingOff = true;
    }

    public void Dispose()
    {
        foreach (var entry in graphics) if (entry.Key != null) entry.Key.enabled = entry.Value;
        foreach (var entry in renderers) if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
        graphics.Clear(); renderers.Clear(); roots = Array.Empty<IntPtr>();
    }
}

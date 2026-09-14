using SiNiSistar2;
using SiNiSistar2.UI;
using UnityEngine;

namespace HeatSeekar;

// Native save menus can have the same sorting order as the loading canvas.
// Rebuilding the canvas stack (including letterbox creation) exposes that tie.
// Own only the transition interval, leaving native layout and alpha untouched.
internal sealed class TransitionLayers : IDisposable
{
    private readonly Dictionary<IntPtr, (Canvas Canvas, int Order)> saved = new();
    private UIList? owner;
    private UIScreenFader? fader;
    private TransitionUI? loading;

    public void Tick()
    {
        var ui = GameContext.Managers?.m_UIList;
        if (ui == null) { Restore(); return; }
        if (owner != ui || fader == null || loading == null)
        {
            Restore();
            owner = ui;
            fader = ui.GetComponentsInChildren<UIScreenFader>(true)
                .FirstOrDefault(item => item.name == "FaderBlack" && item.m_Image != null
                    && item.m_Image.canvas?.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay);
            loading = ui.TransitionUI;
        }
        var loadingVisible = loading != null && loading.GetComponentsInChildren<UnityEngine.UI.Graphic>()
            .Any(graphic => graphic.isActiveAndEnabled && graphic.canvasRenderer.GetInheritedAlpha() * graphic.color.a > 0.001f);
        if ((fader == null || fader.m_Alpha <= 0f) && !loadingVisible) { Restore(); return; }
        var fadeCanvas = fader?.m_Image?.canvas?.rootCanvas;
        var loadingCanvas = loading?.m_Canvas?.rootCanvas;
        if (fadeCanvas == null || loadingCanvas == null) { Restore(); return; }
        var top = 0;
        foreach (var canvas in ui.GetComponentsInChildren<Canvas>())
        {
            if (!canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay
                || canvas == fadeCanvas || canvas == loadingCanvas) continue;
            top = Math.Max(top, canvas.sortingOrder);
        }
        SetOrder(fadeCanvas, Math.Min(top + 1, short.MaxValue - 6));
        SetOrder(loadingCanvas, Math.Min(top + 2, short.MaxValue - 5));
    }

    private void SetOrder(Canvas canvas, int order)
    {
        if (!saved.TryGetValue(canvas.Pointer, out var state))
        {
            state = (canvas, canvas.sortingOrder);
            saved[canvas.Pointer] = state;
        }
        order = Math.Max(order, state.Order);
        if (canvas.sortingOrder != order) canvas.sortingOrder = order;
    }

    private void Restore()
    {
        foreach (var state in saved.Values)
            if (state.Canvas != null) state.Canvas.sortingOrder = state.Order;
        saved.Clear();
    }

    public void Dispose() => Restore();
}

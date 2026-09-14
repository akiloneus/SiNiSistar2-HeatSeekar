using Il2CppInterop.Runtime;
using SiNiSistar2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace HeatSeekar;

// Render directly into an integer-sized viewport. No intermediate low-resolution
// image is stretched to fill the monitor, and Unity keeps screen-to-camera rays valid.
internal sealed class AspectDisplay : IDisposable
{
    private sealed record CameraState(Camera Camera, Rect Rect, float Aspect, bool AutomaticAspect);
    private sealed record RectState(RectTransform Rect, Vector2 Minimum, Vector2 Maximum);
    private sealed record CanvasState(Canvas Canvas, CanvasScaler? Scaler, CanvasScaler.ScreenMatchMode MatchMode, List<RectState> HudChildren);
    private readonly DisplayOptions display;
    private readonly Dictionary<IntPtr, CameraState> cameras = new();
    private readonly Dictionary<IntPtr, CanvasState> canvases = new();
    private GameObject? bars;
    private RectTransform[] barRects = Array.Empty<RectTransform>();
    private float nextCanvasScan;
    private int width, height;
    public Rect PixelRect { get; private set; }
    public bool Active { get; private set; }

    public AspectDisplay(DisplayOptions display) { this.display = display; }

    public void Tick()
    {
        if (!display.Current.PreserveAspect) { Restore(); return; }
        var managers = GameContext.Managers;
        var manager = managers != null ? managers.m_Camera : null;
        if (manager == null || manager.MainCamera == null) return;
        Active = true;
        PixelRect = Fit(Screen.width, Screen.height);
        var rect = new Rect(PixelRect.x / Screen.width, PixelRect.y / Screen.height,
            PixelRect.width / Screen.width, PixelRect.height / Screen.height);
        ApplyCamera(manager.MainCamera, rect);
        ApplyCamera(manager.UICamera, rect);
        ApplyCamera(manager.FrontCamera, rect);
        var resized = width != Screen.width || height != Screen.height;
        width = Screen.width; height = Screen.height;
        if (resized || Time.realtimeSinceStartup >= nextCanvasScan)
        {
            nextCanvasScan = Time.realtimeSinceStartup + 0.5f;
            foreach (var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || canvas != canvas.rootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    || canvas.gameObject.name.StartsWith("HeatSeekar.") || canvases.ContainsKey(canvas.Pointer)) continue;
                var native = canvas.GetComponentsInParent<MonoBehaviour>(true).Any(component => component != null
                    && component.GetIl2CppType().FullName.StartsWith("SiNiSistar2."));
                if (!native) continue;
                // Most HUDs render through UICamera. The quest notification is
                // an overlay HUD, so only its anchors follow the world viewport.
                // Menus and fades keep their full-screen layout, sorting and masks.
                var children = new List<RectState>();
                if (canvas.GetComponentInParent<QuestProgressPopup>(true) != null)
                    for (var index = 0; index < canvas.transform.childCount; index++)
                    {
                        var child = canvas.transform.GetChild(index).TryCast<RectTransform>();
                        if (child != null) children.Add(new(child, child.anchorMin, child.anchorMax));
                    }
                var scaler = canvas.GetComponent<CanvasScaler>();
                canvases[canvas.Pointer] = new(canvas, scaler, scaler != null ? scaler.screenMatchMode : default, children);
            }
        }
        foreach (var state in canvases.Values)
        {
            // Native menus use a fixed design size and usually match height.
            // Fit both dimensions so narrow windows cannot crop their text.
            if (state.Scaler != null && state.Scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                state.Scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            foreach (var child in state.HudChildren)
            {
                if (child.Rect == null) continue;
                child.Rect.anchorMin = rect.position + Vector2.Scale(child.Minimum, rect.size);
                child.Rect.anchorMax = rect.position + Vector2.Scale(child.Maximum, rect.size);
            }
        }
        EnsureBars();
        SetBar(0, 0, 0, width, PixelRect.y);
        SetBar(1, 0, PixelRect.yMax, width, height - PixelRect.yMax);
        SetBar(2, 0, PixelRect.y, PixelRect.x, PixelRect.height);
        SetBar(3, PixelRect.xMax, PixelRect.y, width - PixelRect.xMax, PixelRect.height);
        bars!.SetActive(true);
        if (resized) Canvas.ForceUpdateCanvases();
    }

    internal static Rect Fit(int width, int height)
    {
        var fittedHeight = Math.Max(1, Math.Min(height, width / 2));
        var fittedWidth = fittedHeight * 2;
        return new Rect((width - fittedWidth) / 2, (height - fittedHeight) / 2, fittedWidth, fittedHeight);
    }

    private void ApplyCamera(Camera? camera, Rect rect)
    {
        if (camera == null || camera.targetTexture != null || camera.targetDisplay != 0) return;
        if (!cameras.ContainsKey(camera.Pointer))
        {
            var expected = camera.pixelHeight > 0 ? (float)camera.pixelWidth / camera.pixelHeight : camera.aspect;
            cameras[camera.Pointer] = new(camera, camera.rect, camera.aspect, Math.Abs(camera.aspect - expected) < 0.001f);
        }
        camera.rect = rect;
        camera.aspect = 2f;
    }

    private void EnsureBars()
    {
        if (bars != null) return;
        bars = new GameObject("HeatSeekar.Letterbox", new[] { Il2CppType.Of<RectTransform>() });
        bars.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(bars);
        var canvas = bars.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Cover the camera output outside 2:1, then let menus and external tools
        // draw over the bands. Their native sorting order remains untouched.
        canvas.sortingOrder = short.MinValue;
        barRects = Enumerable.Range(0, 4).Select(index =>
        {
            var rect = new GameObject("Bar" + index, new[] { Il2CppType.Of<RectTransform>() }).GetComponent<RectTransform>();
            rect.SetParent(bars.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }).ToArray();
    }

    private void SetBar(int index, float x, float y, float width, float height)
    {
        barRects[index].anchoredPosition = new Vector2(x, y);
        barRects[index].sizeDelta = new Vector2(width, height);
        barRects[index].gameObject.SetActive(width > 0 && height > 0);
    }

    private void Restore()
    {
        if (!Active && cameras.Count == 0 && canvases.Count == 0) return;
        Active = false;
        foreach (var state in cameras.Values)
        {
            if (state.Camera == null) continue;
            state.Camera.rect = state.Rect;
            if (state.AutomaticAspect) state.Camera.ResetAspect(); else state.Camera.aspect = state.Aspect;
        }
        foreach (var state in canvases.Values)
        {
            if (state.Scaler != null) state.Scaler.screenMatchMode = state.MatchMode;
            foreach (var child in state.HudChildren)
            {
                if (child.Rect == null) continue;
                child.Rect.anchorMin = child.Minimum;
                child.Rect.anchorMax = child.Maximum;
            }
        }
        cameras.Clear(); canvases.Clear();
        if (bars != null) bars.SetActive(false);
        width = height = 0;
        Canvas.ForceUpdateCanvases();
    }

    public void Dispose()
    {
        Restore();
        if (bars != null) UnityEngine.Object.Destroy(bars);
        bars = null;
        barRects = Array.Empty<RectTransform>();
    }
}

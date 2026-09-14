using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine;
using UnityEngine.UI;

namespace HeatSeekar;

// Keep Content/List in place: the native PauseMenu controller animates its
// VerticalLayoutGroup.spacing and Content's alpha. Only its viewport is new.
internal sealed class NativeScrollList : IDisposable
{
    private sealed record RectState(RectTransform Rect, Vector2 Min, Vector2 Max, Vector2 Pivot, Vector2 Position, Vector2 Size)
    {
        internal static RectState Save(RectTransform rect) => new(rect, rect.anchorMin, rect.anchorMax, rect.pivot, rect.anchoredPosition, rect.sizeDelta);
        internal void Restore()
        {
            if (Rect == null) return;
            Rect.anchorMin = Min; Rect.anchorMax = Max; Rect.pivot = Pivot;
            Rect.anchoredPosition = Position; Rect.sizeDelta = Size;
        }
    }
    private readonly ToggleListUI page;
    private readonly RectTransform viewport, list;
    private readonly RectState viewState, listState;
    private readonly List<RectState> decorations = new();
    private readonly Dictionary<MaskableGraphic, bool> masks = new();
    private readonly List<UnityEngine.Object> added = new();
    private readonly VerticalLayoutGroup layout;
    private readonly bool layoutEnabled, controlWidth, controlHeight, expandWidth, expandHeight;
    private readonly TextAnchor alignment;
    private readonly ContentSizeFitter fitter;
    private readonly bool fitterEnabled;
    private readonly ContentSizeFitter.FitMode horizontalFit, verticalFit;
    private readonly ScrollRect scroll;
    private readonly Il2CppStructArray<Vector3> corners = new(4);
    private Toggle? lastSelection;
    private float lastHeight;
    private readonly float restSpacing;
    private Vector2 startPosition;
    private bool wasOpen;

    internal NativeScrollList(ToggleListUI page, RectTransform list, IEnumerable<RectTransform?> chrome, float restSpacing)
    {
        this.page = page; this.list = list;
        this.restSpacing = restSpacing;
        viewport = list.parent.GetComponent<RectTransform>();
        viewState = RectState.Save(viewport); listState = RectState.Save(list);
        foreach (var decoration in chrome.Where(item => item != null).Select(item => item!))
        {
            decorations.Add(RectState.Save(decoration));
            // These are direct children. World transforms can be collapsed by
            // an inactive native parent animation during setup.
            var position = decoration.localPosition - (Vector3)viewport.rect.center;
            decoration.anchorMin = decoration.anchorMax = new Vector2(0.5f, 0.5f);
            decoration.anchoredPosition = new Vector2(position.x, position.y);
            // Headings and help share the native fade but sit outside the list.
            foreach (var graphic in decoration.GetComponentsInChildren<MaskableGraphic>(true))
            { masks[graphic] = graphic.maskable; graphic.maskable = false; }
        }
        viewport.anchorMin = viewport.anchorMax = viewport.pivot = new Vector2(0.5f, 0.5f);
        viewport.anchoredPosition = Vector2.zero;
        viewport.sizeDelta = new Vector2(1000, 520);
        // Preserve the native centered pivot and alignment. Changing these to
        // a top pivot also changes the motion produced by the spacing curves.
        layout = list.GetComponent<VerticalLayoutGroup>();
        if (layout == null) { layout = list.gameObject.AddComponent<VerticalLayoutGroup>(); added.Add(layout); layout.spacing = 30; }
        (layoutEnabled, controlWidth, controlHeight, expandWidth, expandHeight, alignment) =
            (layout.enabled, layout.childControlWidth, layout.childControlHeight, layout.childForceExpandWidth, layout.childForceExpandHeight, layout.childAlignment);
        layout.enabled = true;
        fitter = list.GetComponent<ContentSizeFitter>();
        if (fitter == null) { fitter = list.gameObject.AddComponent<ContentSizeFitter>(); added.Add(fitter); }
        (fitterEnabled, horizontalFit, verticalFit) = (fitter.enabled, fitter.horizontalFit, fitter.verticalFit);
        fitter.enabled = true; fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        added.Add(viewport.gameObject.AddComponent<RectMask2D>());
        var surface = viewport.gameObject.AddComponent<Image>();
        surface.color = Color.clear; surface.raycastTarget = true; added.Add(surface);
        scroll = viewport.gameObject.AddComponent<ScrollRect>(); added.Add(scroll);
        scroll.content = list; scroll.viewport = viewport;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = false; scroll.scrollSensitivity = 70;
        // Keep scrolling and clipping without adding a visible scrollbar.
        Reset();
    }

    internal bool Contains(Toggle toggle) => toggle != null && list != null && toggle.transform.parent == list;
    internal bool AllowsPointer(Vector2 position) => viewport != null && UiGeometry.Contains(viewport, position);

    internal void Tick()
    {
        if (page == null) return;
        if (page.IsOpen != wasOpen) { Reset(); wasOpen = page.IsOpen; }
        if (!page.IsOpen || !page.IsSelectState) return;
        var selected = page.OnCursorToggle;
        if ((selected != lastSelection || !Mathf.Approximately(lastHeight, list.rect.height))
            && Plugin.Runtime?.Menu.IsPointerFocus(page) != true) Reveal(selected);
        lastSelection = selected; lastHeight = list.rect.height;
    }

    internal void Reset()
    {
        var height = (float)layout.padding.vertical;
        var count = 0;
        for (var index = 0; index < list.childCount; index++)
        {
            var child = list.GetChild(index).GetComponent<RectTransform>();
            if (child == null || !child.gameObject.activeSelf || child.GetComponent<LayoutElement>()?.ignoreLayout == true) continue;
            height += child.rect.height; count++;
        }
        height += restSpacing * Mathf.Max(0, count - 1);
        list.sizeDelta = new Vector2(list.sizeDelta.x, height);
        startPosition = listState.Position;
        if (height > viewport.rect.height)
            startPosition.y = viewport.rect.yMax - (1 - list.pivot.y) * height;
        else
            // The native list starts below center. Keep that offset only as
            // far as it allows every row of a fitting list to remain visible.
            startPosition.y = Mathf.Clamp(startPosition.y, viewport.rect.yMin + list.pivot.y * height,
                viewport.rect.yMax - (1 - list.pivot.y) * height);
        list.anchoredPosition = startPosition;
        scroll.StopMovement();
        lastSelection = null; lastHeight = 0;
    }

    internal void Reveal(Toggle? toggle)
    {
        if (toggle == null || !Contains(toggle)) return;
        Canvas.ForceUpdateCanvases();
        // A managed array is copied on the IL2CPP boundary; GetWorldCorners
        // writes into the native array, which must also be used for the reads.
        toggle.GetComponent<RectTransform>().GetWorldCorners(corners);
        var bottom = viewport.InverseTransformPoint(corners[0]).y;
        var top = viewport.InverseTransformPoint(corners[1]).y;
        var offset = top > viewport.rect.yMax ? viewport.rect.yMax - top
            : bottom < viewport.rect.yMin ? viewport.rect.yMin - bottom : 0;
        var position = list.anchoredPosition;
        position.y = Mathf.Clamp(position.y + offset, startPosition.y,
            startPosition.y + Mathf.Max(0, list.rect.height - viewport.rect.height));
        list.anchoredPosition = position;
        scroll.StopMovement();
        lastSelection = toggle; lastHeight = list.rect.height;
    }

    public void Dispose()
    {
        foreach (var pair in masks) if (pair.Key != null) pair.Key.maskable = pair.Value;
        foreach (var state in decorations) state.Restore();
        if (layout != null)
        {
            (layout.enabled, layout.childControlWidth, layout.childControlHeight, layout.childForceExpandWidth, layout.childForceExpandHeight, layout.childAlignment) =
                (layoutEnabled, controlWidth, controlHeight, expandWidth, expandHeight, alignment);
        }
        if (fitter != null) (fitter.enabled, fitter.horizontalFit, fitter.verticalFit) = (fitterEnabled, horizontalFit, verticalFit);
        foreach (var component in added.AsEnumerable().Reverse()) if (component != null) UnityEngine.Object.Destroy(component);
        viewState.Restore(); listState.Restore();
    }
}

using SiNiSistar2.UI;
using UnityEngine;

namespace HeatSeekar;

// The native HUD is laid out in a 1440 x 720 design area. Moving its existing
// groups keeps the gauge, labels, value animations and camera scaling together.
internal sealed class PortraitHiddenLayout : IDisposable
{
    private sealed record Placement(RectTransform Rect, Vector2 Min, Vector2 Max, Vector2 Position, float Bottom)
    {
        internal static Placement Save(RectTransform rect)
        {
            var parent = rect.parent.GetComponent<RectTransform>();
            var bottom = rect.localPosition.y + rect.rect.yMin - parent.rect.yMin;
            return new(rect, rect.anchorMin, rect.anchorMax, rect.anchoredPosition, bottom);
        }

        internal void MoveLeftEdge(float left)
        {
            if (Rect == null) return;
            Rect.anchorMin = Rect.anchorMax = Vector2.zero;
            Rect.anchoredPosition = new Vector2(left - Rect.rect.xMin, Bottom - Rect.rect.yMin);
        }

        internal void Restore()
        {
            if (Rect == null) return;
            Rect.anchorMin = Min; Rect.anchorMax = Max; Rect.anchoredPosition = Position;
        }
    }

    private MainUI? owner;
    private Placement? status, name, relics;

    internal void Apply(MainUI? main, bool hidePortrait)
    {
        if (!hidePortrait || main == null) { Dispose(); return; }
        if (owner != main || status?.Rect == null || name?.Rect == null || relics?.Rect == null)
        {
            Dispose();
            var nameUi = main.NameUI;
            var relicsUi = main.RelicsUI;
            if (nameUi == null || relicsUi == null) return;
            var statusRect = nameUi.transform.parent.Find("Status")?.GetComponent<RectTransform>();
            if (statusRect == null) return;
            owner = main;
            status = Placement.Save(statusRect);
            name = Placement.Save(nameUi.GetComponent<RectTransform>());
            relics = Placement.Save(relicsUi.GetComponent<RectTransform>());
        }

        // Reference layout: gauge at the left edge, Name above Relics to its
        // right, with the relic value aligned to the native HP/MP number line.
        status.MoveLeftEdge(30);
        name.MoveLeftEdge(200);
        relics.MoveLeftEdge(200);
    }

    public void Dispose()
    {
        status?.Restore(); name?.Restore(); relics?.Restore();
        status = name = relics = null;
        owner = null;
    }
}

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
    private Placement? status;

    internal void Apply(MainUI? main, bool hidePortrait)
    {
        if (!hidePortrait || main == null) { Dispose(); return; }
        if (owner != main || status?.Rect == null)
        {
            Dispose();
            var nameUi = main.NameUI;
            if (nameUi == null) return;
            var statusRect = nameUi.transform.parent.Find("Status")?.GetComponent<RectTransform>();
            if (statusRect == null) return;
            owner = main;
            status = Placement.Save(statusRect);
        }

        // Compact layout: keep Name and Relics at their native lower-left position
        // (x = 12..148) and shift Status (Gauge + AbnormalGroup) left next to them
        // so AbnormalGroup stays clear of Name and Relics.
        status.MoveLeftEdge(184);
    }

    public void Dispose()
    {
        status?.Restore();
        status = null;
        owner = null;
    }
}

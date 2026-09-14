using UnityEngine;

namespace HeatSeekar;

internal static class UiGeometry
{
    public static bool Contains(RectTransform rect, Vector2 screenPoint, float padding = 0)
    {
        var canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        canvas = canvas.rootCanvas;
        if (canvas.renderMode == RenderMode.WorldSpace)
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, canvas.worldCamera);

        // GameStackCamera moves in LateUpdate, before Unity aligns its screen-space
        // canvas for rendering. Camera.ScreenPointToRay at that instant combines a
        // new camera transform with the previous canvas transform. Work entirely
        // in canvas coordinates so pointer hits use the same viewport as the UI.
        var viewport = canvas.pixelRect;
        if (!viewport.Contains(screenPoint) || viewport.width <= 0 || viewport.height <= 0) return false;
        var root = canvas.GetComponent<RectTransform>();
        var rootPoint = new Vector3(root.rect.xMin + (screenPoint.x - viewport.x) / viewport.width * root.rect.width,
            root.rect.yMin + (screenPoint.y - viewport.y) / viewport.height * root.rect.height, 0);
        var local = rect.InverseTransformPoint(root.TransformPoint(rootPoint));
        var direction = rect.InverseTransformVector(root.forward);
        if (Mathf.Abs(direction.z) > 0.0001f) local -= direction * (local.z / direction.z);
        var bounds = rect.rect;
        bounds.xMin -= padding; bounds.xMax += padding;
        bounds.yMin -= padding; bounds.yMax += padding;
        return bounds.Contains(new Vector2(local.x, local.y));
    }
}

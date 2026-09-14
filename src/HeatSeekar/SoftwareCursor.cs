using BepInEx.Logging;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace HeatSeekar;

internal sealed class SoftwareCursor : IDisposable
{
    private GameObject? root;
    private RectTransform? pointer;
    private Image? image;
    private readonly Settings settings;
    private readonly CursorImage menuImage;
    private readonly CursorImage aimImage;
    public bool Visible => root != null && root.activeSelf;

    public SoftwareCursor(Settings settings, ManualLogSource log, string? directory = null)
    {
        this.settings = settings;
        var dataRoot = directory ?? PluginData.Root;
        menuImage = new CursorImage(dataRoot, "assets/cursor.png", "HeatSeekar.Assets.cursor.png", log);
        aimImage = new CursorImage(dataRoot, "assets/crosshair.png", "HeatSeekar.Assets.crosshair.png", log);
    }

    public void Show(Vector2 position, float diameter, bool aiming)
    {
        EnsureCreated();
        var source = aiming ? aimImage : menuImage;
        var sprite = source.Get(aiming ? settings.CrosshairImagePath.Value : settings.CursorImagePath.Value, Time.realtimeSinceStartup);
        image!.sprite = sprite;
        var size = Mathf.Round(diameter * Mathf.Clamp(Screen.height / 1080f, 0.65f, 3f));
        var scale = size / Mathf.Max(sprite.rect.width, sprite.rect.height);
        pointer!.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        var x = aiming ? settings.CrosshairHotspotX.Value : settings.CursorHotspotX.Value;
        var y = aiming ? settings.CrosshairHotspotY.Value : settings.CursorHotspotY.Value;
        pointer.pivot = source.UsingFallback ? new Vector2(0.5f, 0.5f) : new Vector2(Hotspot(x), 1f - Hotspot(y));
        pointer.anchoredPosition = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
        image.color = Color.white;
        root!.SetActive(true);
    }

    private static float Hotspot(float value) => float.IsFinite(value) ? Mathf.Clamp01(value) : 0.5f;

    public void Hide() { if (root != null) root.SetActive(false); }

    private void EnsureCreated()
    {
        if (root != null) return;
        root = new GameObject("HeatSeekar.Cursor", new[] { Il2CppType.Of<RectTransform>() });
        root.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(root);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        canvas.pixelPerfect = true;
        var visual = new GameObject("Pointer", new[] { Il2CppType.Of<RectTransform>() });
        pointer = visual.GetComponent<RectTransform>();
        pointer.SetParent(root.transform, false);
        pointer.anchorMin = pointer.anchorMax = Vector2.zero;
        pointer.pivot = new Vector2(0.5f, 0.5f);
        image = visual.AddComponent<Image>();
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    public void Dispose()
    {
        Hide();
        if (root != null) UnityEngine.Object.Destroy(root);
        menuImage.Dispose();
        aimImage.Dispose();
        root = null; pointer = null; image = null;
    }
}

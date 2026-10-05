using Cysharp.Threading.Tasks;
using SiNiSistar2;
using SiNiSistar2.Manager;
using SiNiSistar2.UI;
using SiNiSistar2.UI.Gallery;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameInput = SiNiSistar2.InputManager;

namespace HeatSeekar;

// Gallery has its own action map. Keep the replacement local to that map and
// restore the exact override state when the Gallery UI is no longer open.
internal sealed class GalleryInputBindings : IDisposable
{
    // 1.3.1 adds Gallery content filtering; 1.2.1 has no such property.
    private static readonly System.Reflection.PropertyInfo? hiddenByFilter = typeof(EnemyData).GetProperty("IsHiddenByFilter");
    // Binding paths, visible keyboard labels and the viewer's fallback guides
    // describe the same four controls. Keep those facts together.
    private sealed record Mapping(SiNiInputType Type, InputAction? Action, string Original,
        string Key, string? ViewerGuide = null)
    {
        internal string NativePath => "<Keyboard>/" + Original.ToLowerInvariant();
        internal string EnhancedPath => "<Keyboard>/" + Key;
    }

    private readonly record struct Replacement(InputAction Action, int Index, string? OverridePath);
    private sealed class GuideText
    {
        public Text Component = null!;
        public string Original = "";
        public string Replacement = "";
    }

    private readonly List<Replacement> replacements = new();
    private readonly List<GuideText> guideTexts = new();
    private readonly Mapping[] mappings;
    private ButtonGuideUI? guide;
    private Transform? viewer;
    private bool active;
    private bool wasHidden;
    private int revealedFrame = -1;

    public GalleryInputBindings(GameInput input)
    {
        mappings = new[]
        {
            new Mapping(SiNiInputType.GalleryPrevEnemy, input.GalleryPrevEnemy, "A", "q"),
            new Mapping(SiNiInputType.GalleryNextEnemy, input.GalleryNextEnemy, "D", "e"),
            new Mapping(SiNiInputType.GallerySlow, input.GallerySlow, "S", "l", "Guide_Slow"),
            new Mapping(SiNiInputType.GalleryPause, input.GalleryPause, "Space", "p", "Guide_Stop")
        };
    }

    public bool Active => active;

    public void Tick(bool shouldApply)
    {
        if (shouldApply != active)
        {
            if (shouldApply) Apply();
            else Restore();
        }
        // Native Gallery code can refresh localized guide text after Setup,
        // so keep the visible hints synchronized while the Gallery is open.
        var currentGuide = CurrentGuide();
        if (active && currentGuide != null)
        {
            // Native GalleryEscape can reveal the UI before navigation reads
            // Cancel in LateUpdate. That press must not also leave the page.
            if (wasHidden && !currentGuide.IsUIOff) revealedFrame = Time.frameCount;
            wasHidden = currentGuide.IsUIOff;
            RefreshGuide(currentGuide, true);
        }
    }

    internal bool RevealOnCancel(ToggleListUI owner)
    {
        if (!active || guide == null || owner != guide || !guide.IsSelectState
            || (!guide.IsUIOff && revealedFrame != Time.frameCount)) return false;
        guide.m_IsUIOffProp.Value = false;
        wasHidden = false;
        revealedFrame = -1;
        return true;
    }

    // Native configuration serialization must see the original bindings, not
    // the temporary Gallery remap.
    public void WithNativeBindings(Action operation)
    {
        if (!active)
        {
            operation();
            return;
        }
        var hidden = wasHidden;
        var revealed = revealedFrame;
        Restore();
        try { operation(); }
        finally { Apply(); wasHidden = hidden; revealedFrame = revealed; }
    }

    public void RefreshGuide(ButtonGuideUI target, bool nativeSetup = false)
        => RefreshGuide(target, CurrentAnimationViewer(), nativeSetup);

    internal void RefreshGuide(ButtonGuideUI target, Transform? animationViewer, bool nativeSetup)
    {
        if (!active || target == null) return;
        if (guide != null && guide.Pointer != target.Pointer) RestoreGuideText();
        guide = target;
        viewer = animationViewer;
        var seen = new HashSet<IntPtr>();
        foreach (var icon in GuideIcons())
            RefreshButtonIcon(icon, nativeSetup, seen);
        foreach (var mapping in mappings.Where(item => item.ViewerGuide != null))
        {
            var text = FindGuideButtonText(animationViewer, mapping.ViewerGuide!)
                ?? (mapping.Type == SiNiInputType.GallerySlow ? target.m_SlowModeText : target.m_PauseModeText);
            RefreshGuideText(text, mapping, nativeSetup, seen);
        }
        for (var index = guideTexts.Count - 1; index >= 0; index--)
        {
            var state = guideTexts[index];
            if (state.Component != null && seen.Contains(state.Component.Pointer)) continue;
            RestoreText(state);
            guideTexts.RemoveAt(index);
        }
    }

    public void RefreshButtonIcon(ButtonIcon? icon)
    {
        if (!active || icon == null || !IsGalleryGuide(icon.transform)) return;
        RefreshButtonIcon(icon, false, null);
    }

    private void RefreshButtonIcon(ButtonIcon icon, bool nativeSetup, HashSet<IntPtr>? seen)
    {
        var text = icon.m_ButtonText;
        if (text == null) return;
        var mapping = FindMapping(icon);
        if (mapping == null) return;
        if (icon.m_SiNiInputObject != null)
        {
            var device = icon.m_SiNiInputObject.CalcMainDeviceType();
            if (device != MainDeviceType.Keyboard && device != MainDeviceType.None) return;
        }

        RefreshGuideText(text, mapping, nativeSetup, seen);
    }

    private Mapping? FindMapping(ButtonIcon icon)
    {
        var mapping = mappings.FirstOrDefault(item => item.Type == icon.m_SiNiInputType);
        // Text fallback is only for icons without usable input metadata.
        if (mapping == null && icon.m_SiNiInputObject == null && icon.m_ButtonText != null)
        {
            var label = icon.m_ButtonText.text.Trim();
            mapping = mappings.FirstOrDefault(item => Matches(label, item.Original) || Matches(label, item.Key));
        }
        return mapping;
    }

    internal bool ClickGuide(Vector2 position, ToggleListUI? owner)
    {
        var target = CurrentGuide();
        if (!active || target == null || target != guide || !target.isActiveAndEnabled || !target.IsSelectState
            || target.IsRequestNavigation || (owner != null && owner != target)) return false;

        // The viewer has plain text guides as well as ButtonIcons. Resolve both
        // to the same native control, without adding UI event subscriptions.
        foreach (var mapping in mappings.Where(item => item.ViewerGuide != null))
        {
            var row = FindDescendant(viewer, mapping.ViewerGuide!);
            if (HitGuide(row, position)) { ActivateGuide(target, mapping.Type); return true; }
        }
        foreach (var icon in GuideIcons())
        {
            var type = FindMapping(icon)?.Type ?? icon.m_SiNiInputType;
            if (!IsGalleryControl(type) || !HitGuide(icon.transform, position)) continue;
            ActivateGuide(target, type);
            // A disabled native action still consumes a click on its visible
            // hint; it must never confirm the unrelated focused menu row.
            return true;
        }
        return false;
    }

    private IEnumerable<ButtonIcon> GuideIcons()
    {
        var seen = new HashSet<IntPtr>();
        foreach (var root in new[] { guide?.transform, viewer })
        {
            if (root == null) continue;
            foreach (var icon in root.GetComponentsInChildren<ButtonIcon>(true))
                if (seen.Add(icon.Pointer)) yield return icon;
        }
    }

    private static bool HitGuide(Transform? target, Vector2 position)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        var rect = target.TryCast<RectTransform>();
        if (rect == null || !UiGeometry.Contains(rect, position, 6)) return false;
        // Native Gallery hides whole groups using alpha while leaving their
        // objects active. Invisible hints must not remain click targets.
        for (var node = target; node != null; node = node.parent)
            foreach (var group in node.GetComponents<CanvasGroup>())
                if (group.enabled && group.alpha <= 0.001f) return false;
        return true;
    }

    private static bool IsGalleryControl(SiNiInputType type)
        => type is SiNiInputType.GalleryPrevEnemy or SiNiInputType.GalleryNextEnemy
            or SiNiInputType.GallerySlow or SiNiInputType.GalleryPause or SiNiInputType.GalleryDevMode
            or SiNiInputType.GalleryUIOff or SiNiInputType.GalleryReload or SiNiInputType.GalleryEscape;

    private void ActivateGuide(ButtonGuideUI target, SiNiInputType type)
    {
        if (type == SiNiInputType.GalleryEscape)
        {
            if (target.IsUIOff) target.m_IsUIOffProp.Value = false;
            else if (!target.m_DisableCancel) target._IsCanceled_k__BackingField = true;
            return;
        }
        if (type == SiNiInputType.GalleryUIOff)
        {
            target.m_IsUIOffProp.Value = !target.m_IsUIOffProp.Value;
            wasHidden = target.IsUIOff;
            return;
        }
        if (target.IsUIOff) return;
        switch (type)
        {
            case SiNiInputType.GalleryPrevEnemy:
            case SiNiInputType.GalleryNextEnemy:
                var previous = type == SiNiInputType.GalleryPrevEnemy;
                var enemy = previous ? target.EnemyData?.Prev : target.EnemyData?.Next;
                var seen = new HashSet<IntPtr>();
                while (enemy != null && seen.Add(enemy.Pointer))
                {
                    if (enemy.CategoryData != null && enemy.CategoryData.HasAppear && hiddenByFilter?.GetValue(enemy) is not true)
                    {
                        target.InnerSetEnemyData(enemy);
                        break;
                    }
                    enemy = previous ? enemy.Prev : enemy.Next;
                }
                break;
            case SiNiInputType.GalleryDevMode:
                if (ManagerList.DLC != null && !ManagerList.DLC.IsContentsFiltered)
                    target.m_IsDevelopModeProp.Value = !target.m_IsDevelopModeProp.Value;
                break;
            case SiNiInputType.GallerySlow:
                if (target.m_IsDevelopModeProp.Value) target.m_IsUISlowProp.Value = !target.m_IsUISlowProp.Value;
                break;
            case SiNiInputType.GalleryPause:
                if (target.m_IsDevelopModeProp.Value) target.m_IsUIPauseProp.Value = !target.m_IsUIPauseProp.Value;
                break;
            case SiNiInputType.GalleryReload:
                if (target.m_IsDevelopModeProp.Value && !target.IsReloaded && ManagerList.PostEffect?.Screen?.IsAlphaZero == true)
                    target.Reloaded(ManagerList.RootTokenSource.Token).Forget();
                break;
        }
    }

    private void RefreshGuideText(Text? text, Mapping mapping, bool nativeSetup, HashSet<IntPtr>? seen)
    {
        if (text == null) return;
        seen?.Add(text.Pointer);
        var state = guideTexts.FirstOrDefault(item => item.Component != null && item.Component.Pointer == text.Pointer);
        if (state == null)
        {
            if (!Matches(text.text, mapping.Original) && !Matches(text.text, mapping.Key)) return;
            state = new GuideText
            {
                Component = text,
                Original = Matches(text.text, mapping.Key)
                    ? mapping.Original
                    : text.text,
                Replacement = mapping.Key
            };
            guideTexts.Add(state);
        }
        else if (nativeSetup
            && !Matches(text.text, mapping.Key)
            && !string.Equals(text.text, state.Original, StringComparison.Ordinal))
        {
            state.Original = text.text;
        }

        if (!string.Equals(text.text, state.Replacement, StringComparison.Ordinal))
            text.text = state.Replacement;
    }

    private void Apply()
    {
        if (active) return;
        replacements.Clear();
        foreach (var mapping in mappings) Replace(mapping.Action, mapping.NativePath, mapping.EnhancedPath);
        active = true;
        var currentGuide = CurrentGuide();
        if (currentGuide != null) RefreshGuide(currentGuide);
    }

    private void Replace(InputAction? action, string originalPath, string replacementPath)
    {
        if (action == null) return;
        for (var index = 0; index < action.bindings.Count; index++)
        {
            var binding = action.bindings[index];
            if (!string.Equals(binding.path, originalPath, StringComparison.OrdinalIgnoreCase)) continue;
            replacements.Add(new Replacement(action, index, binding.overridePath));
            action.ApplyBindingOverride(index, replacementPath);
        }
    }

    private void Restore()
    {
        if (!active) return;
        RestoreGuideText();
        for (var index = replacements.Count - 1; index >= 0; index--)
        {
            var replacement = replacements[index];
            if (replacement.OverridePath == null) replacement.Action.RemoveBindingOverride(replacement.Index);
            else replacement.Action.ApplyBindingOverride(replacement.Index, replacement.OverridePath);
        }
        replacements.Clear();
        active = false;
        wasHidden = false;
        revealedFrame = -1;
    }

    private void RestoreGuideText()
    {
        foreach (var state in guideTexts) RestoreText(state);
        guideTexts.Clear();
        guide = null;
        viewer = null;
    }

    private static void RestoreText(GuideText state)
    {
        if (state.Component != null && string.Equals(state.Component.text, state.Replacement, StringComparison.Ordinal))
            state.Component.text = state.Original;
    }

    private bool IsGalleryGuide(Transform target)
        => (guide != null && target.IsChildOf(guide.transform)) || (viewer != null && target.IsChildOf(viewer));

    private static bool Matches(string value, string expected)
        => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static ButtonGuideUI? CurrentGuide()
        => GameContext.Managers?.m_Gallery?.GalleryUI?.ButtonGuideUI;

    private static Transform? CurrentAnimationViewer()
        => GameContext.Managers?.m_Gallery?.GalleryUI?.AnimationViewer?.transform;

    private static Text? FindGuideButtonText(Transform? animationViewer, string guideName)
    {
        var guide = FindDescendant(animationViewer, guideName);
        var buttonText = FindDescendant(guide, "ButtonText");
        if (buttonText == null) return null;
        var text = buttonText.GetComponent<Text>();
        if (text != null) return text;
        return FindDescendant(buttonText, "Text")?.GetComponent<Text>();
    }

    private static Transform? FindDescendant(Transform? parent, string name)
    {
        if (parent == null) return null;
        for (var index = 0; index < parent.childCount; index++)
        {
            var child = parent.GetChild(index);
            if (string.Equals(child.name, name, StringComparison.Ordinal)) return child;
            var nested = FindDescendant(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    public void Dispose()
    {
        Restore();
        replacements.Clear();
    }
}

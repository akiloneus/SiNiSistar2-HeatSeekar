using SiNiSistar2;
using SiNiSistar2.UI;
using SiNiSistar2.UI.Gallery;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameInput = SiNiSistar2.InputManager;

namespace HeatSeekar;

// Gallery has its own action map. Keep the replacement local to that map and
// restore the exact override state when the Gallery UI is no longer open.
internal sealed class GalleryInputBindings : IDisposable
{
    private readonly record struct Replacement(InputAction Action, int Index, string? OverridePath);
    private sealed class GuideText
    {
        public Text Component = null!;
        public string Original = "";
        public string Replacement = "";
    }

    private readonly List<Replacement> replacements = new();
    private readonly List<GuideText> guideTexts = new();
    private readonly InputAction? previous;
    private readonly InputAction? next;
    private readonly InputAction? slow;
    private readonly InputAction? pause;
    private ButtonGuideUI? guide;
    private bool active;

    public GalleryInputBindings(GameInput input)
    {
        previous = input.GalleryPrevEnemy;
        next = input.GalleryNextEnemy;
        slow = input.GallerySlow;
        pause = input.GalleryPause;
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
        if (active && currentGuide != null) RefreshGuide(currentGuide, true);
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
        Restore();
        try { operation(); }
        finally { Apply(); }
    }

    public void RefreshGuide(ButtonGuideUI target, bool nativeSetup = false)
        => RefreshGuide(target, CurrentAnimationViewer(), nativeSetup);

    internal void RefreshGuide(ButtonGuideUI target, Transform? animationViewer, bool nativeSetup)
    {
        if (!active || target == null) return;
        if (guide != null && guide.Pointer != target.Pointer) RestoreGuideText();
        guide = target;
        var seen = new HashSet<IntPtr>();
        foreach (var icon in target.GetComponentsInChildren<ButtonIcon>(true))
            RefreshButtonIcon(icon, nativeSetup, seen);
        RefreshGuideText(FindGuideButtonText(animationViewer, "Guide_Slow") ?? target.m_SlowModeText,
            "S", "l", nativeSetup, seen);
        RefreshGuideText(FindGuideButtonText(animationViewer, "Guide_Stop") ?? target.m_PauseModeText,
            "Space", "p", nativeSetup, seen);
        guideTexts.RemoveAll(item => item.Component == null || !seen.Contains(item.Component.Pointer));
    }

    public void RefreshButtonIcon(ButtonIcon? icon)
    {
        if (!active || icon == null) return;
        RefreshButtonIcon(icon, false, null);
    }

    private void RefreshButtonIcon(ButtonIcon icon, bool nativeSetup, HashSet<IntPtr>? seen)
    {
        var text = icon.m_ButtonText;
        if (text == null || !TryGetReplacement(icon.m_SiNiInputType, text.text, out var original, out var replacement)) return;
        if (icon.m_SiNiInputObject != null)
        {
            var device = icon.m_SiNiInputObject.CalcMainDeviceType();
            if (device != MainDeviceType.Keyboard && device != MainDeviceType.None) return;
        }

        seen?.Add(text.Pointer);
        var state = guideTexts.FirstOrDefault(item => item.Component.Pointer == text.Pointer);
        if (state == null)
        {
            if (!string.Equals(text.text, original, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(text.text, replacement, StringComparison.OrdinalIgnoreCase)) return;
            state = new GuideText
            {
                Component = text,
                Original = string.Equals(text.text, replacement, StringComparison.OrdinalIgnoreCase) ? original : text.text,
                Replacement = replacement
            };
            guideTexts.Add(state);
        }
        else if (nativeSetup
            && !string.Equals(text.text, replacement, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(text.text, state.Original, StringComparison.Ordinal))
        {
            state.Original = text.text;
        }

        if (!string.Equals(text.text, state.Replacement, StringComparison.Ordinal))
            text.text = state.Replacement;
    }

    private void RefreshGuideText(Text? text, string original, string replacement,
        bool nativeSetup, HashSet<IntPtr> seen)
    {
        if (text == null) return;
        seen.Add(text.Pointer);
        var state = guideTexts.FirstOrDefault(item => item.Component.Pointer == text.Pointer);
        if (state == null)
        {
            if (!string.Equals(text.text, original, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(text.text, replacement, StringComparison.OrdinalIgnoreCase)) return;
            state = new GuideText
            {
                Component = text,
                Original = string.Equals(text.text, replacement, StringComparison.OrdinalIgnoreCase)
                    ? original
                    : text.text,
                Replacement = replacement
            };
            guideTexts.Add(state);
        }
        else if (nativeSetup
            && !string.Equals(text.text, replacement, StringComparison.OrdinalIgnoreCase)
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
        Replace(previous, "<Keyboard>/a", "<Keyboard>/q");
        Replace(next, "<Keyboard>/d", "<Keyboard>/e");
        Replace(slow, "<Keyboard>/s", "<Keyboard>/l");
        Replace(pause, "<Keyboard>/space", "<Keyboard>/p");
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
    }

    private void RestoreGuideText()
    {
        foreach (var state in guideTexts)
        {
            if (state.Component == null) continue;
            if (string.Equals(state.Component.text, state.Replacement, StringComparison.Ordinal))
                state.Component.text = state.Original;
        }
        guideTexts.Clear();
        guide = null;
    }

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

    private static bool TryGetReplacement(SiNiInputType type, string currentText,
        out string original, out string replacement)
    {
        switch (type)
        {
            case SiNiInputType.GalleryPrevEnemy:
                original = "A";
                replacement = "q";
                return true;
            case SiNiInputType.GalleryNextEnemy:
                original = "D";
                replacement = "e";
                return true;
            case SiNiInputType.GallerySlow:
                original = "S";
                replacement = "l";
                return true;
            case SiNiInputType.GalleryPause:
                original = "Space";
                replacement = "p";
                return true;
        }

        // Some native Gallery icons do not carry a usable SiNiInputObject.
        // In that case the visible key label is the reliable keyboard hint.
        switch (currentText.Trim().ToUpperInvariant())
        {
            case "A":
            case "Q":
                original = "A";
                replacement = "q";
                return true;
            case "D":
            case "E":
                original = "D";
                replacement = "e";
                return true;
            case "S":
            case "L":
                original = "S";
                replacement = "l";
                return true;
            case "SPACE":
            case "P":
                original = "Space";
                replacement = "p";
                return true;
            default:
                original = "";
                replacement = "";
                return false;
        }
    }

    public void Dispose()
    {
        Restore();
        replacements.Clear();
    }
}

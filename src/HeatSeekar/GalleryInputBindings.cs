using SiNiSistar2;
using SiNiSistar2.UI.Gallery;
using System.Text.RegularExpressions;
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
        if (shouldApply == active) return;
        if (shouldApply) Apply();
        else Restore();
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
    {
        if (!active || target == null) return;
        if (guide != null && guide.Pointer != target.Pointer) RestoreGuideText();
        guide = target;
        var seen = new HashSet<IntPtr>();
        foreach (var text in target.GetComponentsInChildren<Text>(true))
        {
            if (text == null) continue;
            seen.Add(text.Pointer);
            var state = guideTexts.FirstOrDefault(item => item.Component.Pointer == text.Pointer);
            var mapped = MapGuideText(text.text);
            if (state == null)
            {
                if (mapped == text.text) continue;
                state = new GuideText { Component = text, Original = text.text };
                guideTexts.Add(state);
            }
            else if (nativeSetup && text.text != MapGuideText(state.Original))
            {
                state.Original = text.text;
            }
            mapped = MapGuideText(state.Original);
            if (text.text != mapped) text.text = mapped;
        }
        guideTexts.RemoveAll(item => item.Component == null || !seen.Contains(item.Component.Pointer));
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
            var mapped = MapGuideText(state.Original);
            if (state.Component.text == mapped) state.Component.text = state.Original;
        }
        guideTexts.Clear();
        guide = null;
    }

    private static ButtonGuideUI? CurrentGuide()
        => GameContext.Managers?.m_Gallery?.GalleryUI?.ButtonGuideUI;

    private static string MapGuideText(string value)
    {
        var mapped = ReplaceKey(value, "space", "p");
        mapped = ReplaceKey(mapped, "a", "q");
        mapped = ReplaceKey(mapped, "d", "e");
        return ReplaceKey(mapped, "s", "l");
    }

    private static string ReplaceKey(string value, string key, string replacement)
        => Regex.Replace(
            value,
            $@"(?<![A-Za-z0-9]){Regex.Escape(key)}(?![A-Za-z0-9])",
            match => char.IsUpper(match.Value[0]) ? replacement.ToUpperInvariant() : replacement,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public void Dispose()
    {
        Restore();
        replacements.Clear();
    }
}

using SiNiSistar2;
using UnityEngine.InputSystem;
using GameInput = SiNiSistar2.InputManager;

namespace HeatSeekar;

// Gallery has its own action map. Keep the replacement local to that map and
// restore the exact override state when the Gallery UI is no longer open.
internal sealed class GalleryInputBindings : IDisposable
{
    private readonly record struct Replacement(InputAction Action, int Index, string? OverridePath);

    private readonly List<Replacement> replacements = new();
    private readonly InputAction? previous;
    private readonly InputAction? next;
    private readonly InputAction? slow;
    private readonly InputAction? pause;
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

    private void Apply()
    {
        if (active) return;
        replacements.Clear();
        Replace(previous, "<Keyboard>/a", "<Keyboard>/q");
        Replace(next, "<Keyboard>/d", "<Keyboard>/e");
        Replace(slow, "<Keyboard>/s", "<Keyboard>/l");
        Replace(pause, "<Keyboard>/space", "<Keyboard>/p");
        active = true;
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
        for (var index = replacements.Count - 1; index >= 0; index--)
        {
            var replacement = replacements[index];
            if (replacement.OverridePath == null) replacement.Action.RemoveBindingOverride(replacement.Index);
            else replacement.Action.ApplyBindingOverride(replacement.Index, replacement.OverridePath);
        }
        replacements.Clear();
        active = false;
    }

    public void Dispose()
    {
        Restore();
        replacements.Clear();
    }
}

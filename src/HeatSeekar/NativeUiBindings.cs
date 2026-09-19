using UnityEngine.InputSystem;
using GameInput = SiNiSistar2.InputManager;

namespace HeatSeekar;

// Additional UI controls live alongside the game's fixed X/Z and controller
// bindings. Player actions and user binding overrides are never replaced.
internal sealed class NativeUiBindings : IDisposable
{
    private readonly List<(InputAction Action, string Id)> added = new();
    private IntPtr submitPointer;
    private GameInput? input;
    private bool enabled;
    private const string Group = "HeatSeekar.UI";

    public void Attach(GameInput input, bool enabled)
    {
        this.input = input;
        this.enabled = enabled;
        Apply();
    }

    public void SetEnabled(bool value)
    {
        if (enabled == value) return;
        enabled = value;
        Apply();
    }

    private void Apply()
    {
        var manager = input;
        var submit = manager?.UISubmit;
        if (!enabled || manager == null || submit == null)
        {
            RemoveAdded();
            submitPointer = IntPtr.Zero;
            return;
        }
        if (submitPointer != submit.Pointer)
        {
            RemoveAdded();
            submitPointer = submit.Pointer;
        }
        Add(submit, "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/space", "<Keyboard>/j", "<Keyboard>/numpad4", "<Mouse>/leftButton");
        Add(manager.UICancel, "<Keyboard>/escape", "<Keyboard>/numpad5", "<Mouse>/rightButton");
    }

    private void Add(InputAction? action, params string[] paths)
    {
        if (action == null) return;
        foreach (var path in paths)
        {
            var exists = false;
            foreach (var binding in action.bindings)
                if (string.Equals(binding.effectivePath, path, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
            if (exists) continue;
            action.AddBinding(path, groups: Group);
            added.Add((action, action.bindings[action.bindings.Count - 1].id.ToString()));
        }
    }

    private void RemoveAdded()
    {
        foreach (var (action, id) in added)
            for (var index = action.bindings.Count - 1; index >= 0; index--)
                if (action.bindings[index].id.ToString() == id && action.bindings[index].groups == Group)
                { action.ChangeBinding(index).Erase(); break; }
        added.Clear();
    }

    public void Dispose()
    {
        RemoveAdded();
        input = null;
        enabled = false;
        submitPointer = IntPtr.Zero;
    }
}

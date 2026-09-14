using BepInEx.Logging;
using HeatSeekar.Core;
using SiNiSistar2;
using SiNiSistar2.Manager;
using SiNiSistar2.UI.Pause;
using UnityEngine;
using UnityEngine.InputSystem;
using RebindOperation = UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation;

namespace HeatSeekar;

internal sealed class NativeRebinding : IDisposable
{
    private static readonly SiNiInputCompositeType[] compositeTypes = Enum.GetValues<SiNiInputCompositeType>();
    private readonly ManualLogSource log;
    private RebindOperation? operation;
    private SiNiInputObject? owner;
    private int blockedButtons;
    private bool savePending;
    private int completedFrame = -10;
    public bool Active => operation != null;
    internal RebindOperation? CurrentOperation => operation;
    public InputAction? CapturedAction => operation?.action;
    public bool ConsumesInput => Active || Time.frameCount <= completedFrame + 1;

    public NativeRebinding(ManualLogSource log) => this.log = log;

    internal static SiNiInputObject? FindKeyboardOwner(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex < 0) return null;
        var input = GameContext.Managers?.m_InputManager;
        if (input == null || input.InputObjectList == null) return null;
        foreach (var item in input.InputObjectList)
        {
            if (item == null || item.SiNiInputAction?.Pointer != action.Pointer) continue;
            var keyboard = item.InputData?.m_Keyboard;
            if (keyboard == null) continue;
            foreach (var composite in compositeTypes)
                if (keyboard.TryGetResolveActionAndBinding(out var resolved, out var index, composite)
                    && resolved != null && resolved.Pointer == action.Pointer && index == bindingIndex) return item;
        }
        return null;
    }

    public void Attach(RebindOperation rebind, SiNiInputObject? inputObject)
    {
        if (operation != null && operation.Pointer != rebind.Pointer) operation.Cancel();
        operation = rebind;
        owner = inputObject;
        blockedButtons = ReadHeldButtons();

        // WithTargetBinding has already collected the keyboard scheme requirements.
        // Include paths are alternatives, so this admits the mouse without admitting gamepads.
        rebind.WithControlsHavingToMatchPath("<Mouse>");
        rebind.WithControlsExcluding("<Pointer>/position");
        rebind.WithControlsExcluding("<Pointer>/delta");
        rebind.WithControlsExcluding("<Mouse>/clickCount");
        rebind.WithControlsExcluding("<Mouse>/scroll");
        rebind.WithMatchingEventsBeingSuppressed(true);

        var previousComplete = rebind.m_OnComplete;
        var previousCancel = rebind.m_OnCancel;
        var previousMatch = rebind.m_OnPotentialMatch;
        rebind.OnPotentialMatch((Action<RebindOperation>)(current =>
        {
            try
            {
                for (var index = current.candidates.Count - 1; index >= 0; index--)
                {
                    var candidate = current.candidates[index];
                    var bit = MouseButtonBit(candidate);
                    if (bit != 0 && (blockedButtons & bit) != 0) current.RemoveCandidate(candidate);
                }
                if (previousMatch != null) previousMatch.Invoke(current);
                else if (current.candidates.Count > 0) current.Complete();
            }
            catch (Exception error) { log.LogError($"Rebinding candidate processing failed: {error}"); current.Cancel(); }
        }));
        rebind.OnComplete((Action<RebindOperation>)(current =>
        {
            try { previousComplete?.Invoke(current); }
            finally { End(true); }
        }));
        rebind.OnCancel((Action<RebindOperation>)(current =>
        {
            try { previousCancel?.Invoke(current); }
            finally { End(false); }
        }));
        log.LogInfo("Native keyboard rebinding now accepts mouse buttons. Escape cancels; pointer motion is excluded.");
    }

    public void Tick()
    {
        blockedButtons &= ReadHeldButtons();
        var current = operation;
        if (current != null && current.started)
        {
            if (!Application.isFocused) current.Cancel();
            else
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    var wheel = mouse.scroll.ReadValue().y;
                    if (Mathf.Abs(wheel) > 0.1f)
                    {
                        current.AddCandidate(wheel > 0 ? mouse.scroll.up : mouse.scroll.down, 1f, Mathf.Abs(wheel));
                        current.Complete();
                    }
                }
            }
        }
        if (savePending && Time.frameCount > completedFrame)
        {
            savePending = false;
            if (owner != null) owner.UpdateInputAction();
            SaveBindings();
            Plugin.Runtime?.Menu.RefreshBindings();
        }
    }

    private void End(bool changed)
    {
        operation = null;
        completedFrame = Time.frameCount;
        savePending |= changed;
    }

    public static bool TryResolve(InputConfigToggle slot, out InputAction action, out int index)
    {
        action = null!; index = -1;
        var inputObject = slot.m_SiNiInputObject;
        if (inputObject == null || inputObject.InputData == null) return false;
        var binding = inputObject.InputData.GetDeviceBindingTarget(slot.m_SiNiDeviceBaseType);
        return binding != null && binding.TryGetResolveActionAndBinding(out action, out index, slot.m_SiNiInputCompositeType)
            && action != null && index >= 0 && index < action.bindings.Count;
    }

    public static string? Label(SiNiInputObject inputObject, SiNiInputCompositeType composite)
    {
        var data = inputObject.InputData;
        if (data == null || data.m_Keyboard == null) return null;
        if (!data.m_Keyboard.TryGetResolveActionAndBinding(out var action, out var index, composite) || action == null
            || index < 0 || index >= action.bindings.Count) return null;
        return MouseLabels.FromPath(action.bindings[index].effectivePath);
    }

    public bool Edit(InputConfigToggle slot, bool reset)
    {
        if (ConsumesInput || !TryResolve(slot, out var action, out var index)) return false;
        var enabled = action.enabled;
        action.Disable();
        try
        {
            if (reset) action.RemoveBindingOverride(index);
            else action.ApplyBindingOverride(index, string.Empty);
        }
        finally { if (enabled) action.Enable(); }
        var inputObject = slot.m_SiNiInputObject;
        if (reset && inputObject != null) inputObject.InputManager.InGameInputDuplicateUpdate(action.bindings[index]);
        if (inputObject != null) inputObject.UpdateInputAction();
        slot.UpdateInput();
        SaveBindings();
        Plugin.Runtime?.Menu.RefreshBindings();
        log.LogInfo($"{(reset ? "Restored" : "Cleared")} native binding: {action.name}[{index}].");
        return true;
    }

    internal static void SaveBindings()
    {
        Plugin.Runtime?.Configuration.Save();
    }

    private static int ReadHeldButtons()
    {
        var mouse = Mouse.current;
        if (mouse == null) return 0;
        return (mouse.leftButton.isPressed ? 1 : 0) | (mouse.rightButton.isPressed ? 2 : 0)
            | (mouse.middleButton.isPressed ? 4 : 0) | (mouse.backButton.isPressed ? 8 : 0)
            | (mouse.forwardButton.isPressed ? 16 : 0);
    }

    private static int MouseButtonBit(InputControl control)
    {
        if (control.device.TryCast<Mouse>() == null) return 0;
        return control.name switch { "leftButton" => 1, "rightButton" => 2, "middleButton" => 4, "backButton" => 8, "forwardButton" => 16, _ => 0 };
    }

    public void Dispose()
    {
        var current = operation;
        if (current != null) current.Cancel();
        operation = null;
    }
}

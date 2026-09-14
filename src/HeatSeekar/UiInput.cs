using UnityEngine;
using UnityEngine.InputSystem;

namespace HeatSeekar;

internal readonly record struct MenuFrame(Vector2 Move, float Scroll, bool Confirm, bool Cancel, bool Pause,
    bool Click, bool RightClick, bool Clear, bool Reset, bool NonPointerInput);

internal sealed class UiInput : IDisposable
{
    private InputActionMap map = null!;
    private InputAction navigate = null!;
    private readonly List<(InputAction Action, Action Pressed)> buttons = new();
    private InputAction wheel = null!;
    private readonly Il2CppSystem.Action afterUpdate;
    private uint lastUpdate;
    private bool haveUpdate;
    private bool confirm, cancel, pause, click, right, clear, reset;
    private float scroll;
    private int languageDirection;
    private SiNiSistar2.InputManager? native;

    public void SetNativeInput(SiNiSistar2.InputManager input) => native = input;

    public UiInput()
    {
        afterUpdate = (Action)ObserveInputUpdate;
        Rebuild();
        InputSystem.add_onAfterUpdate(afterUpdate);
    }

    public void Rebuild()
    {
        if (map != null) map.Dispose();
        buttons.Clear();
        map = new InputActionMap("HeatSeekar.Menu");
        navigate = map.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
        navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        navigate.AddBinding("<Gamepad>/leftStick");
        navigate.AddBinding("<Gamepad>/dpad");
        var submit = Button("Confirm", () => confirm = true, "<Keyboard>/space", "<Keyboard>/enter", "<Keyboard>/numpadEnter", "<Keyboard>/x",
            "<Keyboard>/j", "<Keyboard>/numpad4");
        var back = Button("Cancel", () => cancel = true, "<Keyboard>/escape", "<Mouse>/rightButton", "<Keyboard>/z",
            "<Keyboard>/numpad5");
        var pauseAction = Button("Pause", () => pause = true, "<Keyboard>/escape");
        Button("Click", () => click = true, "<Mouse>/leftButton");
        Button("RightClick", () => right = true, "<Mouse>/rightButton");
        Button("Clear", () => clear = true, "<Keyboard>/q");
        Button("Reset", () => reset = true, "<Keyboard>/r");
        wheel = map.AddAction("Wheel", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
        if (native != null)
        {
            CopyBindings(native.UINavigate, navigate);
            CopyBindings(native.UISubmit, submit, skipMouse: true);
            CopyBindings(native.UICancel, back);
            CopyBindings(native.Pause, pauseAction);
        }
        else
        {
            submit.AddBinding("<Gamepad>/buttonSouth");
            back.AddBinding("<Gamepad>/buttonEast");
            pauseAction.AddBinding("<Gamepad>/select");
        }
        ClearPending();
        map.Enable();
    }

    private static void CopyBindings(InputAction? source, InputAction destination, bool skipMouse = false)
    {
        if (source == null) return;
        foreach (var binding in source.bindings)
            if (!skipMouse || !binding.effectivePath.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase)) destination.AddBinding(binding);
    }

    private InputAction Button(string name, Action pressed, params string[] paths)
    {
        var action = map.AddAction(name, InputActionType.Button);
        foreach (var path in paths) action.AddBinding(path);
        buttons.Add((action, pressed));
        return action;
    }

    private void ObserveInputUpdate()
    {
        var update = UnityEngine.InputSystem.LowLevel.InputState.updateCount;
        if (haveUpdate && update == lastUpdate) return;
        haveUpdate = true;
        lastUpdate = update;
        var direction = HorizontalDirection();
        if (direction != languageDirection) languageDirection = 0;
        foreach (var button in buttons) if (button.Action.WasPressedThisFrame()) button.Pressed();
        scroll += wheel.ReadValue<Vector2>().y;
    }

    public MenuFrame Read()
    {
        var move = navigate.ReadValue<Vector2>();
        bool keyboard = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        var frame = new MenuFrame(move, scroll, confirm, cancel, pause, click, right, clear, reset,
            move.sqrMagnitude > 0.16f || scroll != 0 || confirm || clear || reset || keyboard
            || (cancel && !right) || (pause && !right));
        ClearPending();
        return frame;
    }

    private int HorizontalDirection()
    {
        var move = navigate.ReadValue<Vector2>();
        return Math.Abs(move.x) > 0.4f && Math.Abs(move.x) >= Math.Abs(move.y) ? Math.Sign(move.x) : 0;
    }

    internal bool ConsumeLanguageDirection()
    {
        var direction = HorizontalDirection();
        if (direction == 0) { languageDirection = 0; return true; }
        if (direction == languageDirection) return false;
        languageDirection = direction;
        return true;
    }

    public void ClearPending() { confirm = cancel = pause = click = right = clear = reset = false; scroll = 0; }
    public void Dispose()
    {
        InputSystem.remove_onAfterUpdate(afterUpdate);
        map.Dispose();
        buttons.Clear();
    }
}

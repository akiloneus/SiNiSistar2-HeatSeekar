using BepInEx.Logging;
using HeatSeekar.Core;
using SiNiSistar2.Manager;
using SiNiSistar2.Obj;
using UnityEngine;
using UnityEngine.InputSystem;
using NVector3 = System.Numerics.Vector3;

namespace HeatSeekar;

internal sealed class Runtime : IDisposable
{
    private readonly Settings settings;
    private readonly ManualLogSource log;
    private readonly SoftwareCursor cursor;
    private readonly TransitionLayers transitionLayers = new();
    private readonly HashSet<string> warnings = new();
    private Camera? camera;
    private Lelia? player;
    private SiNiSistar2.UI.TitleUI? title;
    private bool savedCursor;
    private bool savedVisibility;
    private CursorLockMode savedLock;
    private bool cursorFaulted;
    public NativeRebinding Rebinding { get; }
    public NativeConfiguration Configuration { get; }
    public MenuNavigation Menu { get; }
    public DisplayOptions Display { get; }
    public SettingsModel Options { get; }
    public NativeMenuEntries Entries { get; }
    public Localization Text { get; }
    public NativeSettingsPages Pages { get; }
    public AspectDisplay Aspect { get; }
    public InterfaceOptions Interface { get; }
    private readonly LogConsole console;
    private readonly AimVisuals aimVisuals = new();
    private readonly CombatInput combatInput = new();
    public NativeUiBindings UiBindings { get; } = new();

    public Runtime(Settings settings, ManualLogSource log)
    {
        this.settings = settings; this.log = log;
        cursor = new SoftwareCursor(settings, log);
        Interface = new InterfaceOptions(settings);
        console = new LogConsole(settings, log);
        Text = new Localization(log);
        Configuration = new NativeConfiguration(log);
        Rebinding = new NativeRebinding(log);
        Display = new DisplayOptions(settings, log);
        Aspect = new AspectDisplay(Display);
        Options = new SettingsModel(settings, Display, Text);
        Pages = new NativeSettingsPages(Options, Text, log);
        Menu = new MenuNavigation(Rebinding, log);
        Entries = new NativeMenuEntries(log, Text);
    }

    public void Tick()
    {
        try
        {
            Text.Tick();
            Interface.Tick();
            console.Tick();
            var objects = GameContext.Managers?.m_ObjectManager;
            var aimPlayer = objects != null ? objects.Lelia : null;
            aimVisuals.Apply(aimPlayer != null ? aimPlayer.transform : null, settings.AimEnabled.Value);
            Configuration.Tick();
            Display.Tick();
            Aspect.Tick();
            transitionLayers.Tick();
            Entries.Tick();
            Pages.Tick();
            Rebinding.Tick();
            Menu.Tick(settings.MenuNavigation.Value || Pages.IsOpen);
            var gameplay = GetGameplay();
            var menu = Menu.Active;
            var menuTransition = !menu && Menu.HasOpenPage;
            if (title == null && !gameplay) title = UnityEngine.Object.FindObjectOfType<SiNiSistar2.UI.TitleUI>();
            var titleIdle = !menu && title != null && title.isActiveAndEnabled && title.m_Canvas != null
                && title.m_Canvas.isActiveAndEnabled && title.m_GameMenuUI != null && !title.m_GameMenuUI.IsOpen;
            var mouse = Mouse.current;
            if (!Application.isFocused || ExternalUi.IsOpen)
            {
                cursor.Hide();
                RestoreCursor();
                return;
            }
            if (!savedCursor)
            {
                savedCursor = true; savedVisibility = Cursor.visible; savedLock = Cursor.lockState;
            }
            Cursor.visible = false;
            Cursor.lockState = gameplay && settings.ConfineCursor.Value ? CursorLockMode.Confined : CursorLockMode.None;
            if (mouse == null) { cursor.Hide(); return; }
            var position = mouse.position.ReadValue();
            var show = menu ? Menu.PointerVisible : titleIdle || menuTransition ? Menu.IdlePointerVisible : gameplay && settings.AimEnabled.Value;
            if (gameplay && Aspect.Active) show &= Aspect.PixelRect.Contains(position);
            show &= position.x >= 0 && position.y >= 0 && position.x < Screen.width && position.y < Screen.height;
            if (show && !cursorFaulted)
            {
                try { cursor.Show(position, settings.CursorSize.Value, gameplay); }
                catch (Exception error) { cursorFaulted = true; cursor.Dispose(); Warn("Software cursor", error); }
            }
            else cursor.Hide();
            Cursor.visible = false;
        }
        catch (Exception error)
        {
            Warn("Runtime update", error);
            Menu.Suspend();
            cursor.Hide();
            RestoreCursor();
        }
    }

    internal void InputReady(SiNiSistar2.InputManager input)
    {
        UiBindings.Attach(input);
        Menu.SetInput(input);
    }

    private bool GetGameplay()
    {
        player = null; camera = null;
        var managers = GameContext.Managers;
        if (Pages.IsOpen || managers == null || !ManagerList.HasCompletedFirstInitialize || !ManagerList.HasDoneSceneSetUp
            || managers.m_UIList == null || managers.m_UIList.IsOpenSomeUI || managers.m_ObjectManager == null
            || managers.m_ObjectManager.IsCinematicEvent || managers.m_Camera == null || Rebinding.ConsumesInput) return false;
        var candidate = managers.m_ObjectManager.Lelia;
        var view = managers.m_Camera.MainCamera;
        if (candidate == null || !candidate.isActiveAndEnabled || !candidate.IsSetupEnd || !candidate.IsRunning
            || candidate.IsHold || candidate.TimeScale <= 0 || Time.timeScale <= 0 || view == null
            || !view.isActiveAndEnabled || view.targetDisplay != 0) return false;
        player = candidate; camera = view;
        return true;
    }

    public bool TryAim(MagicArrow shooter, ref Vector3 from, out Vector3 target)
    {
        target = default;
        if (!settings.AimEnabled.Value || !Application.isFocused || ExternalUi.IsOpen || !GetGameplay()
            || player == null || player.MagicArrow != shooter || camera == null || Mouse.current == null) return false;
        var position = Mouse.current.position.ReadValue();
        var aimed = TryPointerTarget(position, from, out target);
        var constrained = AimGeometry.ConstrainToFacing(Convert(from), Convert(target), player.Direction.IsLeft, aimed);
        aimed &= constrained == Convert(target);
        target = new Vector3(constrained.X, constrained.Y, constrained.Z);
        log.LogInfo($"Player arrow target ({(aimed ? "mouse" : "straight ahead")}): ({target.x:F3}, {target.y:F3}, {target.z:F3}).");
        return true;
    }

    private bool TryPointerTarget(Vector2 position, Vector3 from, out Vector3 target)
    {
        target = default;
        if (camera == null || !camera.pixelRect.Contains(position)) return false;
        var ray = camera.ScreenPointToRay(new Vector3(position.x, position.y, 0));
        if (!AimGeometry.TryGetTarget(Convert(ray.origin), Convert(ray.direction), Convert(from), settings.MinimumAimDistance.Value, out var result)) return false;
        target = new Vector3(result.X, result.Y, result.Z);
        return true;
    }

    internal void BeforeNativeInput() => combatInput.Restore();

    internal void ProcessPlayerInput(SiNiSistar2.InputManager input)
    {
        if (!Application.isFocused || ExternalUi.IsOpen || !GetGameplay() || player == null || player.Input != input) return;
        combatInput.Apply(input, settings.AltExplosionTrigger.Value, player.MagicArrow != null && player.MagicArrow.HasExplosion,
            input.Attack != null && input.Attack.IsPressed(), player.MagicArrow?.State ?? MagicArrow.EState.Idle, player.MagicArrow?.IsAction == true);
    }

    internal void AttackStarted(AttackActionBase action)
    {
        if (!settings.AimEnabled.Value || !Application.isFocused || ExternalUi.IsOpen || !GetGameplay()
            || player == null || action.Lelia != player || Mouse.current == null) return;
        if (TryPointerTarget(Mouse.current.position.ReadValue(), player.Position, out var target)
            && Mathf.Abs(target.x - player.Position.x) > settings.MinimumAimDistance.Value)
            TurnAtAttackStart(player.Direction, target);
    }

    internal static void TurnAtAttackStart(SiNiSistar2.Obj.Action.Direction direction, Vector3 target)
    {
        // Clear the pending movement turn that would otherwise overwrite a
        // forced turn at the end of Lelia's update, especially while airborne.
        direction.ClearCall();
        direction.TurnToPosition(target, true);
        direction.Update();
    }

    private static NVector3 Convert(Vector3 value) => new(value.x, value.y, value.z);

    internal void FocusChanged(bool focused)
    {
        Interface.FocusChanged(focused);
        if (focused) return;
        Rebinding.Dispose();
        combatInput.Restore();
        Menu.Suspend();
        cursor.Hide();
        RestoreCursor();
    }

    public void Suspend()
    {
        combatInput.Restore();
        Rebinding.Dispose();
        Pages.Close();
        Menu.Suspend();
        cursor.Hide();
        RestoreCursor();
    }

    private void RestoreCursor()
    {
        if (!savedCursor) return;
        Cursor.lockState = Application.isFocused ? savedLock : CursorLockMode.None;
        Cursor.visible = !Application.isFocused || savedVisibility;
        savedCursor = false;
    }

    public void Warn(string operation, Exception error)
    {
        if (warnings.Add(operation + ":" + error.GetType().Name + ":" + error.Message)) log.LogError($"{operation}: {error}");
    }

    public void Dispose() { Suspend(); aimVisuals.Dispose(); Interface.Dispose(); console.Dispose(); Configuration.Dispose(); Pages.Dispose(); Entries.Dispose(); Menu.Dispose(); UiBindings.Dispose(); Display.Dispose(); Aspect.Dispose(); transitionLayers.Dispose(); cursor.Dispose(); }
}

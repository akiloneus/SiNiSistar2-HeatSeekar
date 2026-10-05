using BepInEx.Logging;
using HeatSeekar.Core;
using SiNiSistar2.UI.Gallery;
using SiNiSistar2.UI.Pause;
using SiNiSistar2.UI;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GameInput = SiNiSistar2.InputManager;

namespace HeatSeekar;

internal sealed class MenuNavigation : IDisposable
{
    private readonly ManualLogSource log;
    private readonly NativeRebinding rebinding;
    private readonly UiInput ui;
    private readonly NativeMenuInteraction interaction;
    private GalleryInputBindings? galleryBindings;
    private readonly MenuPolicy policy = new();
    private readonly MenuPolicy idlePolicy = new();
    private readonly List<ToggleListUI> menus = new();
    private readonly List<InputAction> disabledActions = new();
    private readonly Il2CppSystem.Collections.Generic.List<RaycastResult> raycasts = new();
    private GameInput? input;
    private ToggleListUI? current;
    private Toggle? logical;
    private EventSystem? eventSystem;
    private Vector2 lastPointer;
    private int enteredFrame;
    private float nextScroll;
    private bool rebuildPending;
    private bool suppressed;
    private bool loggedMenu;
    private bool navigationEnabled;
    private bool pointerArmed;
    private int layoutFrame = -1;
    private float nextMenuScan;

    public bool Active => current != null;
    internal bool HasOpenPage => menus.Any(menu => menu != null && menu.isActiveAndEnabled && (menu.IsOpen || menu.IsSelectState));
    public bool PointerVisible => Active && policy.PointerActive && !rebinding.ConsumesInput;
    public ToggleListUI? Current => current;
    internal bool SlotFocused => policy.SlotFocus;
    internal bool IdlePointerVisible => idlePolicy.PointerActive;
    internal bool GalleryPointerVisible => galleryBindings?.Active == true && idlePolicy.PointerActive;

    public MenuNavigation(NativeRebinding rebinding, ManualLogSource log, UiInput ui, NativeMenuInteraction interaction)
    {
        this.rebinding = rebinding; this.log = log;
        this.ui = ui; this.interaction = interaction;
    }

    public void SetInput(GameInput manager)
    {
        if (input != null && input.Pointer == manager.Pointer) return;
        ReleaseOwnership();
        galleryBindings?.Dispose();
        input = manager;
        galleryBindings = new GalleryInputBindings(manager);
        ui.SetNativeInput(manager);
        rebuildPending = true;
    }

    public void RefreshBindings() => rebuildPending = true;
    public void Register(ToggleListUI menu)
    {
        if (!menus.Any(item => item != null && item.Pointer == menu.Pointer)) menus.Add(menu);
    }

    public void Unregister(ToggleListUI menu)
    {
        menus.RemoveAll(item => item == null || item.Pointer == menu.Pointer);
        if (current == menu) SuspendCurrent();
    }

    public void Tick(bool enabled)
    {
        navigationEnabled = enabled;
        if (rebuildPending && !rebinding.Active)
        {
            ui.Rebuild();
            rebuildPending = false;
        }
        var frame = ui.Read();
        var galleryOpen = enabled && IsGalleryOpen();
        galleryBindings?.Tick(galleryOpen);
        var ownGalleryInput = galleryOpen && Application.isFocused && !ExternalUi.IsOpen;
        if (ownGalleryInput)
        {
            // Gallery keys are native controls. Escape still returns through
            // the generic cancel path, while Pause/Clear/Reset stay native.
            frame = frame with { Pause = false, Clear = false, Reset = false };
        }
        var mouse = Mouse.current;
        var position = mouse == null ? Vector2.zero : mouse.position.ReadValue();
        idlePolicy.UpdatePointer((position - lastPointer).sqrMagnitude > 0.25f, frame.NonPointerInput);
        if (!enabled || !Application.isFocused || ExternalUi.IsOpen)
        {
            Suspend();
            lastPointer = position;
            return;
        }
        menus.RemoveAll(menu => menu == null);
        if (Time.realtimeSinceStartup >= nextMenuScan)
        {
            nextMenuScan = Time.realtimeSinceStartup + 0.25f;
            // Some native child menus initialize through inlined setup paths,
            // bypassing the subscription detour used by ordinary pages.
            foreach (var candidatePage in UnityEngine.Object.FindObjectsOfType<ToggleListUI>(true)) Register(candidatePage);
        }
        // IsSelectState is true while the native menu is waiting for a selection.
        // Parent pages stop waiting while a child page or a rebind operation owns input.
        var injected = Plugin.Runtime?.Pages.OpenPage;
        var candidate = injected != null ? injected.IsSelectState ? injected : null
            : menus.LastOrDefault(menu => menu.isActiveAndEnabled && menu.IsOpen && menu.IsSelectState);
        if (candidate == null)
        {
            SuspendCurrent();
            lastPointer = position;
            return;
        }
        if (current != candidate) Enter(candidate, position);
        AcquireOwnership();
        EnsureSuppressed();
        Process(frame, position);
    }

    internal void Enter(ToggleListUI menu, Vector2 position)
    {
        current = menu;
        policy.Enter(idlePolicy.PointerActive);
        pointerArmed = false;
        enteredFrame = Time.frameCount;
        nextScroll = 0;
        lastPointer = position;
        // Native pages decide whether to remember their index when returning
        // from a child or reset it after closing. Do not override that policy.
        logical = menu.OnCursorToggle;
        if (!IsAvailable(logical)) logical = ValidToggles().FirstOrDefault();
        Focus(logical);
        if (!loggedMenu)
        {
            loggedMenu = true;
            log.LogInfo($"Native menu navigation attached: {menu.gameObject.name}. Wheel selects; move the mouse to show the pointer.");
        }
    }

    internal void Process(MenuFrame frame, Vector2 position)
    {
        if (current == null) return;
        var moved = (position - lastPointer).sqrMagnitude > 0.25f;
        lastPointer = position;
        if (Time.frameCount == enteredFrame || rebinding.ConsumesInput || !current.IsSelectState)
        {
            galleryBindings?.ClearHover();
            return;
        }
        SyncLogical();
        // Q/R switch to focus mode, but their target is the slot that was under
        // the visible pointer before that switch, not the entire logical row.
        var editTarget = (frame.Clear || frame.Reset) && ((policy.PointerActive && pointerArmed) || moved) ? HitTest(position) : null;
        policy.UpdatePointer(moved, frame.NonPointerInput);
        if (frame.NonPointerInput) pointerArmed = false;
        else if (moved) pointerArmed = true;
        if (policy.PointerActive) policy.LeaveSlots();
        if (galleryBindings?.Active == true)
        {
            var pointerAvailable = policy.PointerActive && !PointerBlocked(position, ValidToggles());
            galleryBindings.HoverGuide(position, current, pointerAvailable);
            if (frame.Click && pointerAvailable && galleryBindings.ClickGuide(position, current)) return;
        }
        // An explicit new click may act at the pointer even before it moves;
        // the click that opened this page was already consumed during entry.
        var hovered = policy.PointerActive && (pointerArmed || frame.Click) ? HitTest(position) : null;
        if (hovered != null && (hovered != logical || current.OnCursorToggle != hovered))
        {
            policy.LeaveSlots();
            Focus(hovered, reveal: false);
        }

        // A nested binding slot consumes Cancel/Pause before the page can handle it.
        if ((frame.Cancel || frame.Pause) && policy.ConsumeSlotCancel()) return;
        var hoverSlot = hovered == null ? null : Slot(hovered);
        if (frame.Clear || frame.Reset)
        {
            if (frame.Reset && Plugin.Runtime?.Pages.Reset(logical) == true) return;
            var targets = Slot(editTarget) != null ? new[] { editTarget! } : hoverSlot != null ? new[] { hovered! }
                : policy.SlotFocus && logical != null ? new[] { logical } : BindingGroup(logical);
            foreach (var target in targets)
            {
                var slot = Slot(target);
                if (slot != null) rebinding.Edit(slot, frame.Reset);
            }
            if (targets.Length > 0 && Slot(targets[0]) != null) return;
        }
        if (frame.Cancel)
        {
            if (galleryBindings?.RevealOnCancel(current) == true) return;
            Cancel();
            return;
        }
        if (frame.Pause)
        {
            ResumePause();
            return;
        }
        int x = Math.Abs(frame.Move.x) >= Math.Abs(frame.Move.y) && Math.Abs(frame.Move.x) > 0.4f ? Math.Sign(frame.Move.x) : 0;
        int y = x == 0 && Math.Abs(frame.Move.y) > 0.4f ? Math.Sign(frame.Move.y) : 0;
        if (policy.Repeat(x, y, Time.unscaledTime)) Move(x, y);
        if (Math.Abs(frame.Scroll) > 0.1f && Time.unscaledTime >= nextScroll)
        {
            nextScroll = Time.unscaledTime + 0.05f;
            if (policy.SlotFocus) MoveSlot(frame.Scroll > 0 ? -1 : 1);
            else if (Slot(logical) != null) WheelTraverse(frame.Scroll > 0 ? -1 : 1);
            else Traverse(frame.Scroll > 0 ? -1 : 1);
        }
        if (frame.Click && policy.PointerActive)
        {
            if (hovered != null)
            {
                var direction = ArrowDirection(hovered, position);
                if (direction != 0) interaction.Horizontal(current, hovered, direction, pointerClick: true);
                else Confirm(hovered, true);
            }
        }
        else if (frame.Confirm || (frame.Click && !policy.PointerActive)) Confirm(logical, false);
        // No input module may drop the logical selection with an invisible background click.
        if (!policy.PointerActive && logical != null) Focus(logical, reveal: false);
    }

    private void SyncLogical()
    {
        if (current == null) return;
        var native = current.OnCursorToggle;
        if (IsAvailable(native))
        {
            if (logical != native) Focus(native);
            return;
        }
        if (IsAvailable(logical)) return;
        Focus(ValidToggles().FirstOrDefault());
    }

    private void Confirm(Toggle? target, bool pointerClick)
    {
        if (current == null || !IsAvailable(target)) return;
        var group = BindingGroup(target);
        if (!pointerClick && group.Length > 1 && !policy.SlotFocus)
        {
            policy.EnterSlots();
            Focus(target);
            return;
        }
        interaction.Confirm(current, target!);
    }

    private void Cancel()
    {
        if (current == null || current.m_DisableCancel) return;
        current._IsCanceled_k__BackingField = true;
    }

    private void ResumePause()
    {
        var pause = GameContext.Managers?.m_UIList?.PauseMenu;
        if (pause == null || !pause.IsOpen) return;
        foreach (var menu in menus.Where(menu => menu != null && menu.IsOpen)) menu.ForceCloseFlag = true;
        pause.Close();
    }

    private void MoveSlot(int step)
    {
        var group = BindingGroup(logical);
        if (group.Length == 0) return;
        var index = Array.FindIndex(group, toggle => toggle == logical);
        Focus(group[MenuPolicy.Wrap(index, step, group.Length)]);
    }

    internal void Traverse(int step)
    {
        var entries = RowEntries();
        if (entries.Length == 0) return;
        var group = BindingGroup(logical);
        var anchor = group.Length > 0 ? group[0] : logical;
        var index = Array.FindIndex(entries, entry => entry == anchor);
        Focus(entries[MenuPolicy.Wrap(index, step, entries.Length)]);
    }

    private void WheelTraverse(int step)
    {
        var entries = WheelEntries();
        var index = Array.FindIndex(entries, entry => entry == logical);
        if (index < 0) return;
        Focus(entries[MenuPolicy.Wrap(index, step, entries.Length)]);
    }

    private Toggle[] WheelEntries()
    {
        var entries = new List<Toggle>();
        // BindingGroup preserves the native page order. Reordering by Selectable
        // links changes movement slots to Down/Left/Right/Up instead of the
        // visible Up/Down/Right/Left order.
        foreach (var row in RowEntries()) entries.AddRange(BindingGroup(row));
        return entries.ToArray();
    }

    private void Move(int x, int y)
    {
        // The native upgrade list lets players inspect unaffordable upgrades.
        // Unity's spatial Selectable search skips those disabled purchase rows.
        if (current?.TryCast<BlessingEnhanceUI>() != null)
        {
            if (y != 0) Traverse(y > 0 ? -1 : 1);
            return;
        }
        if (policy.SlotFocus)
        {
            if (x != 0) MoveSlot(x);
            return;
        }
        if (logical == null) { Focus(ValidToggles().FirstOrDefault()); return; }
        if (Slot(logical) != null)
        {
            // Native binding pages provide explicit links between their
            // keyboard slots. Follow those links before falling back to row
            // traversal; the latter groups Up/Down/Left/Right into one row.
            var linked = Neighbour(logical, x, y);
            if (linked != null) { Focus(linked); return; }
            if (y != 0) Traverse(y > 0 ? -1 : 1);
            return;
        }
        var next = Neighbour(logical, x, y);
        if (next != null) { Focus(next); return; }
        // Walk the opposite direction to wrap within this row/column, including incomplete grids.
        var edge = logical;
        var seen = new HashSet<IntPtr> { edge.Pointer };
        while (true)
        {
            var opposite = Neighbour(edge, -x, -y);
            if (opposite == null || !seen.Add(opposite.Pointer)) break;
            edge = opposite;
        }
        if (edge != logical) Focus(edge);
        else if (x != 0) interaction.Horizontal(current!, logical, x);
        else Traverse(y > 0 ? -1 : 1);
    }

    private Toggle? Neighbour(Toggle from, int x, int y)
    {
        Selectable? selectable = x > 0 ? from.FindSelectableOnRight() : x < 0 ? from.FindSelectableOnLeft()
            : y > 0 ? from.FindSelectableOnUp() : from.FindSelectableOnDown();
        var toggle = selectable == null ? null : selectable.TryCast<Toggle>();
        return toggle != from && IsAvailable(toggle) && ValidToggles().Any(item => item == toggle) ? toggle : null;
    }

    private Toggle[] ValidToggles()
    {
        if (current == null || current.UsingToggles == null) return Array.Empty<Toggle>();
        return current.UsingToggles.Where(IsAvailable).ToArray();
    }

    private Toggle[] RowEntries()
    {
        var rows = new List<Toggle>();
        var seen = new HashSet<string>();
        foreach (var toggle in ValidToggles())
        {
            var slot = Slot(toggle);
            var key = slot == null ? toggle.Pointer.ToString() : $"binding:{slot.m_SiNiDeviceBaseType}:{slot.m_SiNiInputType}";
            if (seen.Add(key)) rows.Add(toggle);
        }
        return rows.ToArray();
    }

    private Toggle[] BindingGroup(Toggle? toggle)
    {
        var slot = Slot(toggle);
        if (slot == null) return toggle == null ? Array.Empty<Toggle>() : new[] { toggle };
        return ValidToggles().Where(candidate =>
        {
            var other = Slot(candidate);
            return other != null && other.m_SiNiDeviceBaseType == slot.m_SiNiDeviceBaseType && other.m_SiNiInputType == slot.m_SiNiInputType;
        }).ToArray();
    }

    private static InputConfigToggle? Slot(Toggle? toggle)
    {
        if (toggle == null) return null;
        var slot = toggle.GetComponent<InputConfigToggle>();
        return slot != null ? slot : toggle.GetComponentInChildren<InputConfigToggle>(true);
    }

    // Native IsDisableToggle checks the row's own interactable flag. Parent
    // CanvasGroups become non-interactable while waiting for their child menu.
    private bool IsAvailable(Toggle? toggle) => toggle != null && toggle.isActiveAndEnabled
        && (toggle.interactable || current?.TryCast<BlessingEnhanceUI>() != null);

    internal void Focus(Toggle? toggle, bool reveal = true)
    {
        if (current == null || !IsAvailable(toggle)) return;
        var changed = logical != toggle || current.OnCursorToggle != toggle;
        logical = toggle;
        var events = EventSystem.current;
        if (events != null && events.currentSelectedGameObject != toggle!.gameObject) events.SetSelectedGameObject(toggle.gameObject);
        if (current.m_OnCursorToggleProp != null && current.OnCursorToggle != toggle) current.m_OnCursorToggleProp.Value = toggle;
        if (changed) current._IsDirtyAnim_k__BackingField = true;
        if (reveal) Plugin.Runtime?.Pages.Reveal(toggle!);
    }

    internal bool IsPointerFocus(ToggleListUI owner) => current == owner && policy.PointerActive && pointerArmed;

    internal bool SuppressNativePointer(ToggleListUI owner) => owner != null && Application.isFocused && !ExternalUi.IsOpen
        && (navigationEnabled || Plugin.Runtime?.Pages.Owns(owner) == true)
        && menus.Any(menu => menu != null && menu.Pointer == owner.Pointer);

    internal Toggle? HitTest(Vector2 position)
    {
        var toggles = ValidToggles();
        if (PointerBlocked(position, toggles)) return null;
        // Some native pages have no raycastable graphics because the original UI
        // only expects keyboard/gamepad navigation. Their Selectable rectangles
        // still define the correct mouse targets without changing game assets.
        foreach (var toggle in toggles.Reverse())
        {
            if (Plugin.Runtime?.Pages.AllowsPointer(toggle, position) == false) continue;
            var rect = toggle.GetComponent<RectTransform>();
            if ((rect != null && UiGeometry.Contains(rect, position)) || ArrowDirection(toggle, position) != 0) return toggle;
        }
        return null;
    }

    // Both menu rows and Gallery hints respect foreground plugin canvases.
    private bool PointerBlocked(Vector2 position, Toggle[] toggles)
    {
        // Flexible choice dialogs rebuild their layout after animation each
        // frame. Resolve it before hit testing, just as Unity does before drawing.
        if (layoutFrame != Time.frameCount) { Canvas.ForceUpdateCanvases(); layoutFrame = Time.frameCount; }
        var events = EventSystem.current;
        // ChoiceDialog owns its canvas as a child, not as an ancestor.
        var nativeCanvases = toggles.Select(toggle => toggle.GetComponentInParent<Canvas>())
            .Where(canvas => canvas != null).Select(canvas => canvas.rootCanvas.Pointer).ToHashSet();
        if (events != null)
        {
            raycasts.Clear();
            events.RaycastAll(new PointerEventData(events) { position = position }, raycasts);
            foreach (var result in raycasts)
            {
                if (result.gameObject == null) continue;
                // A foreground canvas from another plugin owns this point.
                var canvas = result.gameObject.GetComponentInParent<Canvas>();
                if (canvas == null) continue;
                if (nativeCanvases.Contains(canvas.rootCanvas.Pointer)) break;
                return true;
            }
        }
        return false;
    }

    private static int ArrowDirection(Toggle toggle, Vector2 position)
    {
        var arrow = toggle.transform.Find("Arrow");
        if (arrow == null || !arrow.gameObject.activeInHierarchy) return 0;
        var images = arrow.GetComponentsInChildren<Image>().Where(image => image.isActiveAndEnabled).ToArray();
        foreach (var image in images.Reverse())
            if (UiGeometry.Contains(image.rectTransform, position, 18f)) return image.transform == arrow ? -1 : 1;
        return 0;
    }

    private void AcquireOwnership()
    {
        var events = EventSystem.current;
        if (eventSystem != events)
        {
            ReleaseOwnership();
            eventSystem = events;
        }
        if (!suppressed && input != null)
        {
            foreach (var action in new[] { input.UINavigate, input.UISubmit, input.UICancel, input.UIDelete, input.Pause })
            {
                if (action != null && action.enabled && !disabledActions.Any(item => item.Pointer == action.Pointer))
                    disabledActions.Add(action);
            }
            suppressed = true;
        }
    }

    public void EnsureSuppressed()
    {
        if (!OwnsMenuInput) { ReleaseOwnership(); return; }
        if (!suppressed) return;
        foreach (var action in disabledActions) if (action.enabled) action.Disable();
    }

    private bool OwnsMenuInput => current != null && current.isActiveAndEnabled && current.IsOpen
        && navigationEnabled && current.IsSelectState
        && Application.isFocused && !ExternalUi.IsOpen;

    private static bool IsGalleryOpen()
    {
        var gallery = GameContext.Managers?.m_Gallery;
        return gallery != null && (gallery.IsOpenedUI || gallery.GalleryUI?.IsOpen == true);
    }

    // Skip processing while we supply native menu events, without disabling the
    // module or altering its pointer actions. UniverseLib shares their lifecycle.
    internal bool SuppressModule(BaseInputModule module) => OwnsMenuInput && suppressed
        && eventSystem != null && module.GetComponent<EventSystem>() == eventSystem;

    private void ReleaseOwnership()
    {
        foreach (var action in disabledActions) action.Enable();
        disabledActions.Clear();
        suppressed = false;
        eventSystem = null;
    }

    public void Suspend()
    {
        SuspendCurrent();
    }

    private void SuspendCurrent()
    {
        galleryBindings?.ClearHover();
        current = null;
        logical = null;
        pointerArmed = false;
        ReleaseOwnership();
        policy.Enter(idlePolicy.PointerActive);
        ui.ClearPending();
    }

    public void Dispose()
    {
        Suspend();
        galleryBindings?.Dispose();
        ui.Dispose();
        menus.Clear();
    }

    internal void WithNativeBindings(Action operation)
    {
        if (galleryBindings == null) operation();
        else galleryBindings.WithNativeBindings(operation);
    }

    internal void RefreshGalleryButtonGuide(ButtonGuideUI guide)
        => galleryBindings?.RefreshGuide(guide, true);

    internal void RefreshGalleryButtonIcon(ButtonIcon icon)
        => galleryBindings?.RefreshButtonIcon(icon);
}

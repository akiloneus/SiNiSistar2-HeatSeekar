using SiNiSistar2.UI.Gallery;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using GameInput = SiNiSistar2.InputManager;

namespace HeatSeekar;

// Coordinate the distinct lifetimes of native UI additions, menu ownership,
// rebinding and Gallery overrides. Navigation only supplies the selected page.
internal sealed class MenuInputLifecycle : IDisposable
{
    private readonly NativeUiBindings additions = new();
    private readonly List<InputAction> disabledActions = new();
    private readonly Action<string, Exception> warn;
    private GameInput? input;
    private ToggleListUI? owner;
    private EventSystem? eventSystem;
    private bool navigationEnabled, suppressed, rebuildPending;

    private UiInput Ui { get; }
    internal NativeRebinding Rebinding { get; }
    internal GalleryInputBindings? Gallery { get; private set; }
    internal bool Available => navigationEnabled && Application.isFocused && !ExternalUi.IsOpen;
    private bool OwnsInput => Available && owner != null && owner.isActiveAndEnabled
        && owner.IsOpen && owner.IsSelectState;

    internal MenuInputLifecycle(NativeRebinding rebinding, UiInput ui, Action<string, Exception> warn)
    { Rebinding = rebinding; Ui = ui; this.warn = warn; }

    internal void Attach(GameInput manager, bool enhanced)
    {
        additions.Attach(manager, enhanced);
        if (input != null && input.Pointer == manager.Pointer) return;
        Release();
        Gallery?.Dispose();
        input = manager;
        Gallery = new GalleryInputBindings(manager);
        Ui.SetNativeInput(manager);
        RefreshBindings();
    }

    internal void RefreshBindings() => rebuildPending = true;

    internal MenuFrame Read(bool enhanced, bool pluginPageOpen)
    {
        navigationEnabled = enhanced || pluginPageOpen;
        // Completion updates native bindings before rebuilding our action map;
        // ConsumesInput still protects the completion frame and the next frame.
        Rebinding.Tick();
        additions.SetEnabled(enhanced);
        if (rebuildPending && !Rebinding.Active)
        {
            Ui.Rebuild();
            rebuildPending = false;
        }
        var frame = Ui.Read();
        var gallery = GameContext.Managers?.m_Gallery;
        var galleryOpen = enhanced && gallery != null && (gallery.IsOpenedUI || gallery.GalleryUI?.IsOpen == true);
        Gallery?.Tick(galleryOpen);
        // Gallery's native actions remain authoritative. Remaps last for the
        // open Gallery session, including temporary focus/menu ownership gaps.
        if (galleryOpen && Available) frame = frame with { Pause = false, Clear = false, Reset = false };
        if (!Available) Suspend();
        return frame;
    }

    internal void Select(ToggleListUI page)
    {
        var events = EventSystem.current;
        if (eventSystem != events) Release();
        owner = page;
        eventSystem = events;
        if (!OwnsInput) { Release(); return; }
        if (!suppressed && input != null)
        {
            foreach (var action in new[] { input.UINavigate, input.UISubmit, input.UICancel, input.UIDelete, input.Pause })
                if (action != null && action.enabled && !disabledActions.Any(item => item.Pointer == action.Pointer))
                    disabledActions.Add(action);
            suppressed = true;
        }
        EnsureSuppressed();
    }

    // Native action-map changes can reenable these actions between LateUpdates.
    internal void EnsureSuppressed()
    {
        if (!OwnsInput) { Release(); return; }
        foreach (var action in disabledActions) if (action.enabled) action.Disable();
    }

    // Pointer callbacks can run before a page is selectable. Their guard must
    // not depend on the shorter lifetime of selected-page input ownership.
    internal bool GuardPointer(bool pluginPage) => (navigationEnabled || pluginPage)
        && Application.isFocused && !ExternalUi.IsOpen;

    internal bool SuppressModule(BaseInputModule module) => OwnsInput && suppressed
        && eventSystem != null && module.GetComponent<EventSystem>() == eventSystem;

    private void Release()
    {
        owner = null;
        eventSystem = null;
        suppressed = false;
        foreach (var action in disabledActions)
        {
            try { action.Enable(); }
            catch (Exception error) { warn("Restore native menu action", error); }
        }
        disabledActions.Clear();
    }

    internal void Suspend()
    {
        Release();
        try { Gallery?.ClearHover(); }
        finally { Ui.ClearPending(); }
    }

    internal void WithNativeBindings(Action operation)
    {
        if (Gallery == null) operation();
        else Gallery.WithNativeBindings(operation);
    }

    internal void RefreshGalleryButtonGuide(ButtonGuideUI guide) => Gallery?.RefreshGuide(guide, true);
    internal void RefreshGalleryButtonIcon(SiNiSistar2.UI.ButtonIcon icon) => Gallery?.RefreshButtonIcon(icon);

    public void Dispose()
    {
        try { Suspend(); }
        finally
        {
            try { Gallery?.Dispose(); }
            finally
            {
                Gallery = null;
                try { additions.Dispose(); }
                finally { Ui.Dispose(); input = null; }
            }
        }
    }
}

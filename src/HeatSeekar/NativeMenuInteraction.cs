using SiNiSistar2.UI.Pause;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HeatSeekar;

// Navigation supplies a target; native patches supply callbacks. Keep their
// shared page semantics here, while pages own layout and setting values.
internal sealed class NativeMenuInteraction
{
    private readonly NativeMenuEntries entries;
    private readonly NativeSettingsPages pages;
    private readonly Func<bool> consumeLanguageDirection;
    private readonly Action<string, Exception> warn;
    private bool pointerHorizontal;

    internal NativeMenuInteraction(NativeMenuEntries entries, NativeSettingsPages pages,
        Func<bool> consumeLanguageDirection, Action<string, Exception> warn)
    {
        this.entries = entries;
        this.pages = pages;
        this.consumeLanguageDirection = consumeLanguageDirection;
        this.warn = warn;
    }

    internal void Confirm(ToggleListUI owner, Toggle target)
    {
        var events = EventSystem.current;
        if ((owner.IsSelfHandlingActive || owner.TryCast<GamePlayUI>() != null) && events != null)
        {
            // Gameplay uses the ordinary selection loop, but writes settings
            // through Toggle.onValueChanged. Submit must reach UGUI before the
            // native selection callback. BaseEventData is not a pointer event,
            // so navigation's pointer suppression does not consume this submit.
            ExecuteEvents.Execute(target.gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
        }
        else owner.Select(target);
    }

    // Both the Select detour and IL2CPP's inlined submit callback use this gate.
    // Returning true continues native selection, including the parent page's
    // child-page loop; returning false means the plugin handled or blocked it.
    internal bool BeforeSelect(ToggleListUI owner, Toggle target)
    {
        try
        {
            if (entries.IsEntry(target)) return pages.RouteOpen(owner, target);
            return !pages.Activate(owner, target);
        }
        catch (Exception error) { warn("Native settings action", error); return true; }
    }

    internal void Horizontal(ToggleListUI owner, Toggle target, int direction, bool pointerClick = false)
    {
        var previous = pointerHorizontal;
        pointerHorizontal = pointerClick;
        try
        {
            // Injected rows consume the change here. Only native rows reach
            // OnInputHorizontal and its BeforeHorizontal callback below.
            if (!pages.Horizontal(target, direction)) owner.OnInputHorizontal(target, direction);
        }
        finally { pointerHorizontal = previous; }
    }

    internal bool BeforeHorizontal(ToggleListUI owner, Toggle target, int direction)
    {
        var settings = owner.TryCast<SettingUI>();
        if (settings != null)
        {
            // Language loading releases navigation ownership. Guard the native
            // entry as well, so a held direction cannot become a second press.
            return target != settings.m_Language || pointerHorizontal || consumeLanguageDirection();
        }
        return !pages.Horizontal(target, direction);
    }
}

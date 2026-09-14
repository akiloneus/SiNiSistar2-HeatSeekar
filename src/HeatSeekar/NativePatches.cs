using System.Reflection;
using HarmonyLib;
using SiNiSistar2;
using SiNiSistar2.Manager;
using SiNiSistar2.Obj;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine;
using UnityEngine.InputSystem;
using RebindOperation = UnityEngine.InputSystem.InputActionRebindingExtensions.RebindingOperation;

namespace HeatSeekar;

[HarmonyPatch(typeof(SystemSaveData), nameof(SystemSaveData.TrySaveOne))]
internal static class NativeProfileIsolationPatch
{
    private static void Prefix(SystemSaveData __instance, out NativeConfiguration.SaveState? __state)
        => __state = Plugin.Runtime?.Configuration.BeforeSave(__instance);
    private static void Finalizer(SystemSaveData __instance, NativeConfiguration.SaveState? __state)
        => NativeConfiguration.AfterSave(__instance, __state);
}

[HarmonyPatch(typeof(InputActionRebindingExtensions), nameof(InputActionRebindingExtensions.PerformInteractiveRebinding))]
internal static class NativeRebindingFactoryPatch
{
    // ChangeButton returns a non-blittable UniTask and cannot be detoured safely
    // by this game's IL2CPP bridge. Identify the native slot at the class-returning factory instead.
    private static void Postfix(InputAction __0, int __1, RebindOperation __result)
    {
        try
        {
            var owner = NativeRebinding.FindKeyboardOwner(__0, __1);
            if (owner != null) Plugin.Runtime?.Rebinding.Attach(__result, owner);
        }
        catch (Exception error) { Plugin.Runtime?.Warn("Native rebinding setup", error); }
    }
}

[HarmonyPatch(typeof(SiNiInputObject), nameof(SiNiInputObject.GetKeyboardLabel))]
internal static class MouseBindingLabelPatch
{
    private static void Postfix(SiNiInputObject __instance, SiNiInputCompositeType __1, ref string __result)
    {
        try { __result = NativeRebinding.Label(__instance, __1) ?? __result; }
        catch (Exception error) { Plugin.Runtime?.Warn("Mouse binding label", error); }
    }
}

[HarmonyPatch(typeof(SiNiSistar2.InputManager), nameof(SiNiSistar2.InputManager.IsConfigTarget))]
internal static class EmptyBindingValidationPatch
{
    // The original startup validator resets every override when it finds duplicates.
    // Multiple intentionally cleared slots are not duplicate input controls.
    private static void Postfix(InputBinding __0, ref bool __result)
    {
        if (string.IsNullOrEmpty(__0.effectivePath)) __result = false;
    }
}

[HarmonyPatch(typeof(SiNiSistar2.InputManager), nameof(SiNiSistar2.InputManager.SetCurrentActionMap))]
internal static class InputReadyPatch
{
    private static void Postfix(SiNiSistar2.InputManager __instance) => Plugin.Runtime?.InputReady(__instance);
}

[HarmonyPatch(typeof(SiNiSistar2.InputManager), nameof(SiNiSistar2.InputManager.InputUpdate))]
internal static class InputOwnershipPatch
{
    private static void Prefix()
    {
        Plugin.Runtime?.BeforeNativeInput();
        Plugin.Runtime?.Menu.EnsureSuppressed();
    }
    private static void Postfix(SiNiSistar2.InputManager __instance)
    {
        try { Plugin.Runtime?.ProcessPlayerInput(__instance); }
        catch (Exception error) { Plugin.Runtime?.Warn("Player input options", error); }
    }
}

[HarmonyPatch(typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule), nameof(UnityEngine.InputSystem.UI.InputSystemUIInputModule.Process))]
internal static class MenuInputModulePatch
{
    private static bool Prefix(UnityEngine.InputSystem.UI.InputSystemUIInputModule __instance)
        => Plugin.Runtime?.Menu.SuppressModule(__instance) != true;
}

[HarmonyPatch(typeof(SiNiInputObject), nameof(SiNiInputObject.UpdateInputAction))]
internal static class ReboundActionPatch
{
    private static void Postfix() => Plugin.Runtime?.Menu.EnsureSuppressed();
}

[HarmonyPatch(typeof(ToggleListUI), nameof(ToggleListUI.SubscriptToggles))]
internal static class MenuRegistrationPatch
{
    private static void Postfix(ToggleListUI __instance)
    {
        Plugin.Runtime?.Menu.Register(__instance);
        try { Plugin.Runtime?.Entries.Register(__instance); }
        catch (Exception error) { Plugin.Runtime?.Warn("Settings menu entry", error); }
        try { Plugin.Runtime?.Pages.Register(__instance); }
        catch (Exception error) { Plugin.Runtime?.Warn("Native graphics options", error); }
    }
}

[HarmonyPatch(typeof(ToggleListUI), nameof(ToggleListUI.Select))]
internal static class SettingsEntryPatch
{
    internal static bool Prefix(ToggleListUI __instance, UnityEngine.UI.Toggle __0)
    {
        var runtime = Plugin.Runtime;
        if (runtime == null) return true;
        try
        {
            if (runtime.Entries.IsEntry(__0)) return runtime.Pages.RouteOpen(__instance, __0);
            return !runtime.Pages.Activate(__instance, __0);
        }
        catch (Exception error) { runtime.Warn("Native settings action", error); return true; }
    }
}

[HarmonyPatch(typeof(SiNiSistar2.UI.Pause.VideoUI), nameof(SiNiSistar2.UI.Pause.VideoUI.OnInputHorizontal))]
internal static class GraphicsHorizontalPatch
{
    private static bool Prefix(UnityEngine.UI.Toggle __0, int __1) => Plugin.Runtime?.Pages.Horizontal(__0, __1) != true;
}

[HarmonyPatch(typeof(SiNiSistar2.UI.Pause.AudioUI), nameof(SiNiSistar2.UI.Pause.AudioUI.OnInputHorizontal))]
internal static class AudioHorizontalPatch
{
    private static bool Prefix(UnityEngine.UI.Toggle __0, int __1) => Plugin.Runtime?.Pages.Horizontal(__0, __1) != true;
}

[HarmonyPatch(typeof(SiNiSistar2.UI.Pause.SettingUI), nameof(SiNiSistar2.UI.Pause.SettingUI.OnInputHorizontal))]
internal static class LanguageDirectionPatch
{
    // Language loading temporarily releases menu ownership. Guard the native
    // entry too, so a held controller direction cannot become a second press.
    private static bool Prefix(SiNiSistar2.UI.Pause.SettingUI __instance, UnityEngine.UI.Toggle __0)
        => __0 != __instance.m_Language || Plugin.Runtime?.Menu.AllowLanguageHorizontal() != false;
}

[HarmonyPatch(typeof(SiNiSistar2.UI.Pause.VideoUI), nameof(SiNiSistar2.UI.Pause.VideoUI.Setup))]
internal static class NativeVideoReadyPatch
{
    private static void Postfix(SiNiSistar2.UI.Pause.VideoUI __instance) => Plugin.Runtime?.Pages.Ready(__instance);
}

[HarmonyPatch(typeof(SiNiSistar2.UI.Pause.VideoUI), nameof(SiNiSistar2.UI.Pause.VideoUI.UpdateVideoParameter))]
internal static class NativeVideoUpdatePatch
{
    private static bool Prefix(SiNiSistar2.UI.Pause.VideoUI __instance)
        => Plugin.Runtime?.Pages.Owns(__instance) != true && Plugin.Runtime?.Display.UpdateNativeVideo(__instance) != true;
}

// IL2CPP inlines Select in both native pointer/submit subscriptions. These two
// callbacks share one native implementation, so patch that implementation once.
[HarmonyPatch]
internal static class NativeSettingsSubmitPatch
{
    private static MethodBase TargetMethod() => NativeMenuCallbacks.Submit;

    private static bool Prefix(object __instance, UnityEngine.EventSystems.BaseEventData __0)
    {
        var owner = NativeMenuCallbacks.Owner(__instance);
        return !(__0?.TryCast<UnityEngine.EventSystems.PointerEventData>() != null
            && Plugin.Runtime?.Menu.SuppressNativePointer(owner) == true)
            && SettingsEntryPatch.Prefix(owner, NativeMenuCallbacks.Toggle(__instance));
    }
}

[HarmonyPatch]
internal static class NativePointerSelectionPatch
{
    private static MethodBase TargetMethod() => NativeMenuCallbacks.Selection;

    // Unity can process a pointer on the first selectable frame, before our
    // LateUpdate acquires the page. Pointer selection uses the same gate then.
    private static bool Prefix(object __instance, UnityEngine.EventSystems.BaseEventData __0)
        => __0?.TryCast<UnityEngine.EventSystems.PointerEventData>() == null
            || Plugin.Runtime?.Menu.SuppressNativePointer(NativeMenuCallbacks.Owner(__instance)) != true;
}

[HarmonyPatch(typeof(ToggleListUI), nameof(ToggleListUI.OnDestroy))]
internal static class MenuDestroyPatch
{
    private static void Prefix(ToggleListUI __instance) => Plugin.Runtime?.Menu.Unregister(__instance);
}

[HarmonyPatch(typeof(MagicArrow), nameof(MagicArrow._CreateArrow))]
internal static class ArrowAimPatch
{
    private static void Prefix(MagicArrow __instance, ref Vector3 __0, ref Vector3 __1)
    {
        try
        {
            if (Plugin.Runtime != null && Plugin.Runtime.TryAim(__instance, ref __0, out var target)) __1 = target;
        }
        catch (Exception error) { Plugin.Runtime?.Warn("Arrow aiming", error); }
    }
}

// This native entry is called only after Lelia's movement/recovery checks accept
// an action. A rising IsAction covers melee, close magic and the start of a draw.
[HarmonyPatch(typeof(AttackActionBase), nameof(AttackActionBase.UpdateAction))]
internal static class AttackStartDirectionPatch
{
    private static void Prefix(AttackActionBase __instance, out bool __state) => __state = __instance.IsAction;
    private static void Postfix(AttackActionBase __instance, bool __state)
    {
        try
        {
            if (!__state && __instance.IsAction) Plugin.Runtime?.AttackStarted(__instance);
        }
        catch (Exception error) { Plugin.Runtime?.Warn("Attack start direction", error); }
    }
}

using System.Reflection;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HeatSeekar;

internal static class NativeMenuCallbacks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private static readonly Type closure = FindClosure();
    private static readonly PropertyInfo owner = closure.GetProperty("__4__this", Members)!;
    private static readonly PropertyInfo toggle = closure.GetProperty("toggle", Members)!;

    internal static MethodBase Selection => Callback(closure, "_SubscriptToggles_b__0", typeof(BaseEventData))!;
    internal static MethodBase Submit => Callback(closure, "_SubscriptToggles_b__4", typeof(PointerEventData))!;
    internal static ToggleListUI Owner(object instance) => (ToggleListUI)owner.GetValue(instance)!;
    internal static Toggle Toggle(object instance) => (Toggle)toggle.GetValue(instance)!;

    private static Type FindClosure()
    {
        // The compiler renumbers this closure when unrelated ToggleListUI members
        // change (113 in game 1.2.1, 118 in 1.3.1). Resolve its callback contract
        // without putting a version-specific type in Harmony attributes or signatures.
        var matches = typeof(ToggleListUI).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(type => type.GetProperty("__4__this", Members) is { CanRead: true } ownerProperty
                && ownerProperty.PropertyType == typeof(ToggleListUI)
                && type.GetProperty("toggle", Members) is { CanRead: true } toggleProperty
                && toggleProperty.PropertyType == typeof(Toggle)
                && Callback(type, "_SubscriptToggles_b__0", typeof(BaseEventData))?.ReturnType == typeof(void)
                && Callback(type, "_SubscriptToggles_b__4", typeof(PointerEventData))?.ReturnType == typeof(void)
                && Callback(type, "_SubscriptToggles_b__6", typeof(BaseEventData))?.ReturnType == typeof(void))
            .ToArray();
        if (matches.Length != 1)
            throw new NotSupportedException($"Unsupported ToggleListUI.SubscriptToggles callbacks: expected one matching closure, found {matches.Length}.");
        return matches[0];
    }

    private static MethodInfo? Callback(Type type, string name, Type eventType)
        => type.GetMethod(name, Members, null, new[] { eventType }, null);
}

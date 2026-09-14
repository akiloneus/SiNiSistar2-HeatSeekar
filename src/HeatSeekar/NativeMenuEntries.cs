using BepInEx.Logging;
using SiNiSistar2.UI.Pause;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine;
using UnityEngine.UI;

namespace HeatSeekar;

internal sealed class NativeMenuEntries : IDisposable
{
    private sealed record Entry(ToggleListUI Page, Toggle Toggle, Text[] Labels, Toggle Previous, Selectable? PreviousDown,
        Toggle Next, Selectable? NextUp, Vector2? NextPosition);
    private readonly Dictionary<IntPtr, Entry> entries = new();
    private readonly HashSet<IntPtr> installing = new();
    private readonly ManualLogSource log;
    private readonly Localization text;
    private bool disposed;

    public NativeMenuEntries(ManualLogSource log, Localization text) { this.log = log; this.text = text; }

    public void Register(ToggleListUI page)
    {
        if (disposed || page.TryCast<SettingUI>() == null || entries.ContainsKey(page.Pointer) || !installing.Add(page.Pointer)) return;
        GameObject? clone = null;
        try
        {
            var originals = page.UsingToggles?.Where(toggle => toggle != null).ToArray();
            if (originals == null || originals.Length == 0) return;
            // Keep Language at the bottom and insert a regular page button
            // immediately before it, in both layout and native navigation.
            var template = originals[0];
            var language = page.Cast<SettingUI>().m_Language;
            var index = Array.IndexOf(originals, language);
            if (index < 0) index = originals.Length - 1;
            var next = originals[index];
            var previous = originals[(index + originals.Length - 1) % originals.Length];
            clone = UnityEngine.Object.Instantiate(template.gameObject, next.transform.parent, false);
            clone.transform.SetSiblingIndex(next.transform.GetSiblingIndex());
            clone.name = "HeatSeekar";
            var toggle = clone.GetComponent<Toggle>();
            toggle.interactable = true;
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.group = page.m_ToggleGroup;
            foreach (var component in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var name = component.GetIl2CppType().FullName;
                if (component.TryCast<Selectable>() == null && component.TryCast<Graphic>() == null
                    && (name.StartsWith("SiNiSistar2.") || name.Contains("Localiz"))) UnityEngine.Object.Destroy(component);
            }
            var labels = clone.GetComponentsInChildren<Text>(true).ToArray();
            foreach (var label in labels) label.text = text.Text("ui_label_heatseekar");
            var rect = clone.GetComponent<RectTransform>();
            var nextRect = next.GetComponent<RectTransform>();
            Vector2? nextPosition = null;
            if (rect != null && nextRect != null && next.transform.parent.GetComponent<LayoutGroup>() == null)
            {
                var delta = nextRect.anchoredPosition - previous.GetComponent<RectTransform>().anchoredPosition;
                if (delta.y >= -1) delta = Vector2.down * (nextRect.rect.height + 8);
                nextPosition = nextRect.anchoredPosition;
                rect.anchoredPosition = nextRect.anchoredPosition;
                nextRect.anchoredPosition += delta;
            }
            var lastNavigation = previous.navigation;
            var nextNavigation = next.navigation;
            entries[page.Pointer] = new Entry(page, toggle, labels, previous, lastNavigation.selectOnDown, next, nextNavigation.selectOnUp, nextPosition);
            page.m_Toggles = new[] { toggle };
            try { page.SubscriptToggles(); }
            finally { page.m_Toggles = originals.Take(index).Append(toggle).Concat(originals.Skip(index)).ToArray(); }
            if (lastNavigation.mode == Navigation.Mode.Explicit)
            {
                var addedNavigation = toggle.navigation;
                addedNavigation.selectOnUp = previous;
                addedNavigation.selectOnDown = next;
                toggle.navigation = addedNavigation;
                lastNavigation.selectOnDown = toggle;
                previous.navigation = lastNavigation;
                nextNavigation.selectOnUp = toggle;
                next.navigation = nextNavigation;
            }
            log.LogInfo("HeatSeekar settings entry added to a native settings page.");
        }
        catch
        {
            if (entries.Remove(page.Pointer, out var entry)) RemoveEntry(entry);
            else if (clone != null) UnityEngine.Object.Destroy(clone);
            throw;
        }
        finally { installing.Remove(page.Pointer); }
    }

    public bool IsEntry(Toggle toggle) => toggle != null && entries.Values.Any(entry => entry.Toggle != null && entry.Toggle == toggle);

    public void Tick()
    {
        foreach (var key in entries.Where(pair => pair.Value.Page == null || pair.Value.Toggle == null).Select(pair => pair.Key).ToArray()) entries.Remove(key);
        foreach (var entry in entries.Values)
            if (entry.Page.isActiveAndEnabled)
                foreach (var label in entry.Labels)
                    if (label != null) { label.text = text.Text("ui_label_heatseekar"); if (text.Font != null) label.font = text.Font; }
    }

    public void Dispose()
    {
        disposed = true;
        foreach (var entry in entries.Values) RemoveEntry(entry);
        entries.Clear();
    }

    private static void RemoveEntry(Entry entry)
    {
        if (entry.Previous != null)
        {
            var value = entry.Previous.navigation;
            if (value.selectOnDown == entry.Toggle) { value.selectOnDown = entry.PreviousDown; entry.Previous.navigation = value; }
        }
        if (entry.Next != null)
        {
            var value = entry.Next.navigation;
            if (value.selectOnUp == entry.Toggle) { value.selectOnUp = entry.NextUp; entry.Next.navigation = value; }
            if (entry.NextPosition is { } position) entry.Next.GetComponent<RectTransform>().anchoredPosition = position;
        }
        if (entry.Page != null)
        {
            entry.Page.m_Toggles = entry.Page.UsingToggles.Where(toggle => toggle != entry.Toggle).ToArray();
            if (entry.Page.OnCursorToggle == entry.Toggle && entry.Page.m_OnCursorToggleProp != null)
                entry.Page.m_OnCursorToggleProp.Value = entry.Previous != null ? entry.Previous : entry.Next;
        }
        if (entry.Toggle != null) UnityEngine.Object.Destroy(entry.Toggle.gameObject);
    }
}

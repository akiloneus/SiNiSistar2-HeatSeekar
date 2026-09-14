using BepInEx.Logging;
using Il2CppInterop.Runtime;
using SiNiSistar2.UI.Pause;
using SiNiSistar2.UI.ToggleParts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HeatSeekar;

internal sealed class NativeSettingsPages : IDisposable
{
    private sealed record Visual(ToggleListUI Page, Toggle Toggle, SettingsModel.Row Option, Text Label, Text? Value, GameObject? Check);
    private sealed class GraphicsPage
    {
        public VideoUI Page = null!;
        public Toggle[] Originals = Array.Empty<Toggle>();
        public List<Toggle> Added = new();
        public GameObject Template = null!;
        public float RestSpacing;
        public Dictionary<Toggle, Navigation> Navigation = new();
        public Dictionary<Transform, int> Siblings = new();
        public Dictionary<Toggle, (Toggle.ToggleEvent Event, Graphic? Graphic)> Values = new();
        public Text? Help;
        public NativeScrollList? Scroll;
    }
    private sealed record AuxiliaryPage(ToggleListUI Page, Toggle[] Originals, Toggle? Added, Text Help, string FixedHelp)
    {
        public Dictionary<Toggle, Navigation> Navigation { get; } = Originals.ToDictionary(toggle => toggle, toggle => toggle.navigation);
    }

    private readonly SettingsModel options;
    private readonly Localization text;
    private readonly ManualLogSource log;
    private readonly Dictionary<IntPtr, GraphicsPage> graphics = new();
    private readonly Dictionary<IntPtr, Visual> visuals = new();
    private readonly HashSet<IntPtr> installing = new();
    private readonly Dictionary<Text, VerticalWrapMode> valueOverflow = new();
    private readonly Dictionary<IntPtr, AuxiliaryPage> auxiliary = new();
    private VideoUI? page;
    private SettingUI? parent;
    private Toggle? nativeVideoEntry;
    private VideoUI? nativeVideoPage;
    private Text? title, help;
    private ToggleListUI? pageSource;
    private NativeScrollList? ownScroll;
    private Il2CppSystem.IDisposable? pageClosedSubscription;
    private bool childEntered, pageWasOpen;
    private bool disposed;
    public bool IsOpen => page != null && (page.IsOpen || parent != null);
    public ToggleListUI? OpenPage => IsOpen ? page : null;
    public bool Owns(ToggleListUI? candidate) => candidate != null && page != null && candidate == page;

    public NativeSettingsPages(SettingsModel options, Localization text, ManualLogSource log)
    { this.options = options; this.text = text; this.log = log; }

    public void Register(ToggleListUI candidate)
    {
        if (disposed || candidate == null || candidate.name.StartsWith("HeatSeekar.")) return;
        var audio = candidate.TryCast<AudioUI>();
        if (!disposed && audio != null)
            foreach (var label in new[] { audio.m_BGMText, audio.m_VoiceText, audio.m_SEText })
                AllowValueOverflow(label, restore: true);
        if (audio != null) RegisterAudio(audio);
        else if (candidate.UsingToggles != null && candidate.UsingToggles.Any(toggle => toggle != null && toggle.GetComponent<InputConfigToggle>() != null))
            RegisterBindingHelp(candidate);
        var video = candidate.TryCast<VideoUI>();
        if (disposed || video == null || graphics.ContainsKey(video.Pointer) || !installing.Add(video.Pointer)) return;
        try
        {
            var original = video.UsingToggles.Where(toggle => toggle != null).ToArray();
            if (original.Length == 0 || video.m_Resolution == null || video.m_FullScreen == null) return;
            var state = new GraphicsPage { Page = video, Originals = original };
            graphics.Add(video.Pointer, state);
            // Capture the complete native prefab before adding plugin rows or
            // replacing Video's value listeners. Its inactive parent prevents
            // Awake from running on this template or subscribing a second time.
            var storage = new GameObject("HeatSeekar.NativeTemplates");
            storage.SetActive(false);
            storage.transform.SetParent(video.transform.parent, false);
            state.Template = UnityEngine.Object.Instantiate(video.gameObject, storage.transform, false);
            state.Template.name = "HeatSeekar.VideoTemplate";
            state.Template.SetActive(false);
            var openClip = video.m_Animator.runtimeAnimatorController.animationClips.FirstOrDefault(clip => clip.name == video.m_InAnimationName || clip.name.EndsWith("_In"));
            if (openClip != null) openClip.SampleAnimation(state.Template, openClip.length);
            state.RestSpacing = state.Template.transform.Find("Content/List").GetComponent<VerticalLayoutGroup>().spacing;
            BindVisual(video, video.m_Resolution, options.Options.First(option => option.Name == "Resolution"), false);
            BindVisual(video, video.m_FullScreen, options.Options.First(option => option.Name == "Fullscreen"), false);
            BindVisual(video, video.m_VSync, options.Options.First(option => option.Name == "V-sync"), false);
            foreach (var option in options.Options.Where(option => option.Video && option.Name is not ("Resolution" or "Fullscreen" or "V-sync")))
            {
                var template = option.IsOn != null ? video.m_FullScreen : video.m_Resolution;
                var toggle = CloneToggle(template, video.m_Resolution.transform.parent, video.m_ToggleGroup, "HeatSeekar." + option.Name);
                state.Added.Add(toggle);
                BindVisual(video, toggle, option, true);
            }
            var allRows = original.Where(toggle => toggle != video.m_SetDefault).Concat(state.Added)
                .Concat(video.m_SetDefault != null ? new[] { video.m_SetDefault } : Array.Empty<Toggle>()).ToArray();
            // SubscriptToggles adds subscriptions; it does not replace them.
            // Originals already subscribed before our registration postfix.
            video.m_Toggles = state.Added.ToArray();
            try { video.SubscriptToggles(); }
            finally { video.m_Toggles = allRows; }
            LayoutGraphics(state);
            log.LogInfo("HeatSeekar video options added to the native Graphics page.");
        }
        finally { installing.Remove(candidate.Pointer); }
    }

    private void RegisterAudio(AudioUI audio)
    {
        if (auxiliary.ContainsKey(audio.Pointer)) return;
        // Title settings are inactive during child setup. Include inactive
        // ancestors so their Audio page receives the same option as Pause.
        var video = audio.GetComponentInParent<SettingUI>(true)?.m_VideoUI;
        var original = audio.UsingToggles?.Where(toggle => toggle != null).ToArray();
        if (video == null || original == null || original.Length == 0) return;
        var last = original[^1];
        var row = CloneToggle(video.m_FullScreen, last.transform.parent, audio.m_ToggleGroup, "HeatSeekar.Mute in background");
        var rect = row.GetComponent<RectTransform>();
        var lastRect = last.GetComponent<RectTransform>();
        rect.sizeDelta = lastRect.sizeDelta;
        var rowLabel = row.transform.Find("TextBase/Text")?.GetComponent<Text>();
        var lastLabel = last.transform.Find("TextBase/Text")?.GetComponent<Text>();
        if (rowLabel != null && lastLabel != null) rowLabel.fontSize = lastLabel.fontSize;
        if (last.transform.parent.GetComponent<LayoutGroup>() == null)
        {
            rect.anchorMin = lastRect.anchorMin; rect.anchorMax = lastRect.anchorMax; rect.pivot = lastRect.pivot;
            rect.localScale = lastRect.localScale;
            var step = original.Length > 1 ? lastRect.anchoredPosition - original[^2].GetComponent<RectTransform>().anchoredPosition : Vector2.down * 72;
            if (step.y >= -1) step = Vector2.down * 72;
            rect.anchoredPosition = lastRect.anchoredPosition + step;
        }
        var allRows = original.Append(row).ToArray();
        var template = video.m_FullScreen.GetComponentsInChildren<Text>(true).First();
        var footer = NewText("HeatSeekar.AudioHelp", audio.transform.Find("Content") ?? audio.transform, template, 17, new Vector2(0, -316), new Vector2(1200, 48));
        auxiliary[audio.Pointer] = new(audio, original, row, footer, "");
        BindVisual(audio, row, options.Options.Single(option => option.Audio), true);
        audio.m_Toggles = new[] { row };
        try { audio.SubscriptToggles(); }
        finally { audio.m_Toggles = allRows; }
        LinkRows(allRows);
    }

    private void RegisterBindingHelp(ToggleListUI owner)
    {
        if (auxiliary.ContainsKey(owner.Pointer)) return;
        var template = owner.GetComponentsInChildren<Text>(true).FirstOrDefault();
        if (template == null) return;
        var footer = NewText("HeatSeekar.BindingHelp", owner.transform.Find("Content") ?? owner.transform, template, 17, new Vector2(0, -316), new Vector2(1200, 48));
        auxiliary[owner.Pointer] = new(owner, Array.Empty<Toggle>(), null, footer,
            "ui_help_bindings");
    }

    public void Ready(VideoUI video)
    {
        if (Owns(video)) return;
        if (!graphics.TryGetValue(video.Pointer, out var state) || state.Values.Count > 0) return;
        foreach (var toggle in new[] { video.m_FullScreen, video.m_VSync })
        {
            state.Values[toggle] = (toggle.onValueChanged, toggle.graphic);
            // Native submit toggles isOn before selecting the row. Detach only
            // its value listeners so the shared option model applies it once.
            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.graphic = null;
        }
    }

    private void LayoutGraphics(GraphicsPage state)
    {
        Canvas.ForceUpdateCanvases();
        var container = state.Page.m_Resolution.transform.parent;
        var rows = state.Page.UsingToggles.Where(toggle => toggle != null && toggle.gameObject.activeSelf).ToArray();
        state.Siblings = state.Originals.ToDictionary(toggle => toggle.transform, toggle => toggle.transform.GetSiblingIndex());
        for (var index = 0; index < rows.Length; index++)
        {
            state.Navigation[rows[index]] = rows[index].navigation;
            rows[index].transform.SetSiblingIndex(index);
        }
        LinkRows(rows);
        var template = state.Page.m_FullScreen.GetComponentsInChildren<Text>(true).First();
        state.Help = NewText("HeatSeekar.GraphicsHelp", container.parent, template, 17, new Vector2(0, -316), new Vector2(1200, 48));
        state.Scroll = new NativeScrollList(state.Page, container.GetComponent<RectTransform>(),
            new[] { state.Page.transform.Find("Content/Title")?.GetComponent<RectTransform>(), state.Help.rectTransform }, state.RestSpacing);
    }

    public bool Open(ToggleListUI source)
    {
        if (source == null || !source.IsSelectState) return false;
        var entry = source.UsingToggles.FirstOrDefault(toggle => Plugin.Runtime?.Entries.IsEntry(toggle) == true);
        if (entry == null) return false;
        source.Select(entry);
        return IsOpen;
    }

    internal bool RouteOpen(ToggleListUI source, Toggle entry)
    {
        if (parent == source && !childEntered && source.IsSelectState) return true;
        if (disposed || IsOpen || source == null || !source.IsSelectState || ExternalUi.IsOpen || source.TryCast<SettingUI>() == null) return false;
        if (page == null || pageSource != source)
        {
            ReleasePage();
            CreatePage(source);
        }
        if (page == null) return false;
        parent = source.Cast<SettingUI>();
        nativeVideoEntry = parent.m_Video;
        nativeVideoPage = parent.m_VideoUI;
        // Let SettingUI's own selection loop close its list, await the copied
        // child page, and reopen itself. Restore this slot after that round trip.
        parent.m_Video = entry;
        parent.m_VideoUI = page;
        childEntered = false;
        ownScroll?.Reset();
        Plugin.Runtime!.Menu.Register(page);
        Refresh();
        return true;
    }

    private void CreatePage(ToggleListUI source)
    {
        var video = source.Cast<SettingUI>().m_VideoUI;
        if (video == null || !graphics.TryGetValue(video.Pointer, out var state)) return;
        var root = UnityEngine.Object.Instantiate(state.Template, video.transform.parent, false);
        root.name = "HeatSeekar.NativePage";
        root.SetActive(false);
        pageSource = source;
        page = root.GetComponent<VideoUI>();
        RemoveLocalization(root);
        var originals = page.GetComponentsInChildren<Toggle>(true).ToArray();
        var content = root.transform.Find("Content");
        var list = content.Find("List").GetComponent<RectTransform>();
        var fontTemplate = page.m_FullScreen.GetComponentsInChildren<Text>(true).First();
        title = content.Find("Title").GetComponentsInChildren<Text>(true).First();
        help = NewText("Help", content, fontTemplate, 17, new Vector2(0, -300), new Vector2(1150, 70));
        var definitions = options.Options.Where(option => !option.Video && !option.Audio).Concat(new[]
        {
            new SettingsModel.Row { Name = "Back", LabelKey = "ui_label_back", HelpKey = "ui_help_back", Activate = RequestClose }
        }).ToArray();
        var toggles = new List<Toggle>();
        foreach (var option in definitions)
        {
            var template = option.IsOn != null ? page.m_FullScreen : option.Change != null ? page.m_Resolution : page.m_SetDefault;
            var toggle = CloneToggle(template, list, page.m_ToggleGroup, "HeatSeekar." + option.Name);
            toggles.Add(toggle);
            BindVisual(page, toggle, option, true);
        }
        // Keep Video's serialized references valid for its native Setup, but
        // only the replacement rows participate in layout or navigation.
        foreach (var original in originals) original.gameObject.SetActive(false);
        page.m_Toggles = toggles.ToArray();
        page.m_FirstToggle = toggles[0];
        page.m_CancelToggles = Array.Empty<Toggle>();
        page.Setup();
        foreach (var hidden in new[] { page.m_FullScreen, page.m_VSync, page.m_DisplayTextLog })
            if (hidden != null) hidden.onValueChanged = new Toggle.ToggleEvent();
        ownScroll = new NativeScrollList(page, list, new[] { content.Find("Title").GetComponent<RectTransform>(), help.rectTransform }, state.RestSpacing);
        LinkRows(toggles.ToArray());
        pageClosedSubscription = UniRx.ObservableExtensions.Subscribe(page.OnAfterCloseSubject.Cast<Il2CppSystem.IObservable<ToggleListUI>>(),
            (Il2CppSystem.Action<ToggleListUI>)(Action<ToggleListUI>)(_ => { if (disposed) ReleasePage(); }));
    }

    private static Toggle CloneToggle(Toggle source, Transform container, ToggleGroup group, string name)
    {
        var clone = UnityEngine.Object.Instantiate(source.gameObject, container, false);
        clone.name = name;
        RemoveLocalization(clone);
        var toggle = clone.GetComponent<Toggle>();
        toggle.group = group;
        toggle.onValueChanged = new Toggle.ToggleEvent();
        toggle.interactable = true;
        toggle.graphic = null;
        clone.SetActive(true);
        return toggle;
    }

    private static void RemoveLocalization(GameObject owner)
    {
        foreach (var component in owner.GetComponentsInChildren<MonoBehaviour>(true))
            if (component.GetIl2CppType().FullName.StartsWith("SiNiSistar2.Lc."))
            { component.enabled = false; UnityEngine.Object.Destroy(component); }
    }

    private void BindVisual(ToggleListUI owner, Toggle toggle, SettingsModel.Row option, bool cloned)
    {
        var labelTransform = toggle.transform.Find("TextBase/Text");
        var label = labelTransform != null ? labelTransform.GetComponent<Text>() : toggle.GetComponentsInChildren<Text>(true).First();
        var valueTransform = toggle.transform.Find("Value/Text");
        var value = valueTransform != null ? valueTransform.GetComponent<Text>() : null;
        AllowValueOverflow(value, restore: !cloned);
        var checkTransform = toggle.transform.Find("Check");
        var check = checkTransform != null ? checkTransform.gameObject : null;
        var arrow = toggle.transform.Find("Arrow");
        if (cloned && arrow != null) arrow.gameObject.SetActive(option.Change != null && option.IsOn == null);
        if (cloned && option.IsOn == null)
        {
            var square = toggle.transform.Find("Square");
            if (square != null) square.gameObject.SetActive(false);
        }
        visuals[toggle.Pointer] = new(owner, toggle, option, label, value, check);
    }

    private void AllowValueOverflow(Text? label, bool restore)
    {
        if (label == null) return;
        if (restore && !valueOverflow.ContainsKey(label)) valueOverflow[label] = label.verticalOverflow;
        // CJK font metrics and high-resolution rounding can exceed the native
        // value box height. Truncate then removes the entire line of digits.
        // Keep its original dimensions, position and font size.
        label.verticalOverflow = VerticalWrapMode.Overflow;
    }

    public bool Activate(ToggleListUI owner, Toggle toggle)
    {
        var video = owner.TryCast<VideoUI>();
        if (video != null && toggle == video.m_SetDefault) { options.ResetVideoDefaults(); Refresh(); return true; }
        if (toggle == null || !visuals.TryGetValue(toggle.Pointer, out var visual)) return false;
        if (visual.Option.Activate != null) visual.Option.Activate();
        else visual.Option.Change?.Invoke(1);
        Refresh();
        return true;
    }

    public bool Horizontal(Toggle toggle, int direction)
    {
        if (toggle == null || !visuals.TryGetValue(toggle.Pointer, out var visual)) return false;
        visual.Option.Change?.Invoke(direction);
        Refresh();
        return true;
    }

    public bool Reset(Toggle? toggle)
    {
        if (toggle == null || !visuals.TryGetValue(toggle.Pointer, out var visual) || visual.Option.Reset == null) return false;
        visual.Option.Reset(); Refresh(); return true;
    }

    public void Tick()
    {
        foreach (var key in graphics.Where(pair => pair.Value.Page == null).Select(pair => pair.Key).ToArray()) graphics.Remove(key);
        foreach (var key in auxiliary.Where(pair => pair.Value.Page == null).Select(pair => pair.Key).ToArray()) auxiliary.Remove(key);
        foreach (var key in visuals.Where(pair => pair.Value.Page == null || pair.Value.Toggle == null).Select(pair => pair.Key).ToArray()) visuals.Remove(key);
        if (page != null && page.IsOpen) childEntered = pageWasOpen = true;
        if (pageWasOpen && (page == null || !page.IsOpen))
        {
            pageWasOpen = false;
            ClearClosedPage();
        }
        if (parent != null && (page == null || !parent.IsOpen || (childEntered && !page.IsOpen && parent.IsSelectState))) RestoreRoute();
        Refresh();
        ownScroll?.Tick();
        foreach (var state in graphics.Values) state.Scroll?.Tick();
    }

    private void Refresh()
    {
        foreach (var visual in visuals.Values)
        {
            if (visual.Toggle == null || visual.Label == null || !visual.Toggle.gameObject.activeInHierarchy) continue;
            visual.Label.text = text.Text(visual.Option.LabelKey);
            if (text.Font != null) visual.Label.font = text.Font;
            if (visual.Value != null) { visual.Value.text = visual.Option.Value(); if (text.Font != null) visual.Value.font = text.Font; }
            if (visual.Check != null)
            {
                var on = visual.Option.IsOn?.Invoke() == true;
                visual.Check.SetActive(on);
                if (on)
                    foreach (var graphic in visual.Check.GetComponentsInChildren<Graphic>()) graphic.canvasRenderer.SetAlpha(1);
            }
        }
        foreach (var state in graphics.Values)
        {
            if (state.Page == null || !state.Page.IsOpen || state.Help == null) continue;
            var focused = state.Page.OnCursorToggle;
            state.Help.text = options.Description(focused != null && visuals.TryGetValue(focused.Pointer, out var visual) ? visual.Option : null);
            if (text.Font != null) state.Help.font = text.Font;
        }
        foreach (var state in auxiliary.Values)
        {
            if (state.Page == null || !state.Page.IsOpen || state.Help == null) continue;
            var focused = state.Page.OnCursorToggle;
            state.Help.text = state.FixedHelp.Length > 0
                ? Plugin.Runtime?.Rebinding.ConsumesInput == true ? "" : text.Text("ui_prefix_function") + " " + text.Text(state.FixedHelp)
                : options.Description(focused != null && visuals.TryGetValue(focused.Pointer, out var auxiliaryVisual) ? auxiliaryVisual.Option : null);
            if (text.Font != null) state.Help.font = text.Font;
        }
        if (!IsOpen) return;
        title!.text = text.Text("ui_label_heatseekar");
        var selected = page!.OnCursorToggle;
        help!.text = options.Description(selected != null && visuals.TryGetValue(selected.Pointer, out var item) ? item.Option : null, includePrefix: false);
        if (text.Font != null) title.font = help.font = text.Font;
    }

    public void Close()
    {
        RestoreRoute();
        if (page != null && page.IsOpen) RequestClose();
        else ReleasePage();
    }

    private void RequestClose()
    {
        if (page == null) return;
        // Native code samples ForceCloseFlag before waiting for input. An
        // already selectable page must wake that wait through normal Cancel.
        if (page.IsSelectState) page._IsCanceled_k__BackingField = true;
        else if (!page.IsCanceled) page.ForceCloseFlag = true;
    }

    private void RestoreRoute()
    {
        if (parent != null)
        {
            if (nativeVideoEntry != null) parent.m_Video = nativeVideoEntry;
            if (nativeVideoPage != null) parent.m_VideoUI = nativeVideoPage;
        }
        parent = null; nativeVideoEntry = null; nativeVideoPage = null; childEntered = false;
    }

    private void ClearClosedPage()
    {
        if (page == null) return;
        var eventSystem = EventSystem.current;
        var selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        if (selected != null && selected.transform.IsChildOf(page.transform)) eventSystem!.SetSelectedGameObject(null);
        page.ResetDefaultIndex();
        page.m_OnCursorToggleProp.Value = null!;
        page._SelectedToggle_k__BackingField = null!;
        ownScroll?.Reset();
    }

    private void ReleasePage()
    {
        pageClosedSubscription?.Dispose(); pageClosedSubscription = null;
        ownScroll?.Dispose(); ownScroll = null;
        if (page != null)
        {
            Plugin.Runtime?.Menu.Unregister(page);
            UnityEngine.Object.Destroy(page.gameObject);
        }
        page = null; pageSource = null; pageWasOpen = false;
    }

    private NativeScrollList? ScrollFor(Toggle toggle) => ownScroll?.Contains(toggle) == true ? ownScroll
        : graphics.Values.Select(state => state.Scroll).FirstOrDefault(scroll => scroll?.Contains(toggle) == true);

    internal void Reveal(Toggle toggle) => ScrollFor(toggle)?.Reveal(toggle);
    internal bool AllowsPointer(Toggle toggle, Vector2 position) => ScrollFor(toggle)?.AllowsPointer(position) != false;

    private static void LinkRows(Toggle[] rows)
    {
        for (var index = 0; index < rows.Length; index++)
            rows[index].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = rows[(index + rows.Length - 1) % rows.Length], selectOnDown = rows[(index + 1) % rows.Length] };
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = new GameObject(name, new[] { Il2CppType.Of<RectTransform>() }).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Text NewText(string name, Transform parent, Text template, int size, Vector2 position, Vector2 dimensions)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = dimensions;
        var label = rect.gameObject.AddComponent<Text>();
        label.font = template.font; label.fontSize = size; label.color = template.color;
        label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; label.supportRichText = false;
        return label;
    }

    public void Dispose()
    {
        disposed = true;
        foreach (var entry in valueOverflow) if (entry.Key != null) entry.Key.verticalOverflow = entry.Value;
        valueOverflow.Clear();
        Close();
        foreach (var state in auxiliary.Values)
        {
            if (state.Page == null) continue;
            if (state.Added != null)
            {
                state.Page.m_Toggles = state.Originals;
                foreach (var entry in state.Navigation) if (entry.Key != null) entry.Key.navigation = entry.Value;
                UnityEngine.Object.Destroy(state.Added.gameObject);
            }
            if (state.Help != null) UnityEngine.Object.Destroy(state.Help.gameObject);
        }
        auxiliary.Clear();
        foreach (var state in graphics.Values)
        {
            if (state.Page == null) continue;
            state.Scroll?.Dispose();
            state.Page.m_Toggles = state.Originals;
            foreach (var entry in state.Values)
                if (entry.Key != null) { entry.Key.onValueChanged = entry.Value.Event; entry.Key.graphic = entry.Value.Graphic; }
            foreach (var entry in state.Navigation) if (entry.Key != null) entry.Key.navigation = entry.Value;
            foreach (var entry in state.Siblings.OrderBy(entry => entry.Value)) if (entry.Key != null) entry.Key.SetSiblingIndex(entry.Value);
            foreach (var toggle in state.Added) if (toggle != null) UnityEngine.Object.Destroy(toggle.gameObject);
            if (state.Help != null) UnityEngine.Object.Destroy(state.Help.gameObject);
            if (state.Template != null) UnityEngine.Object.Destroy(state.Template.transform.parent.gameObject);
            state.Page.UpdateVideoParameter();
        }
        if (page != null && !page.IsOpen) ReleasePage();
        graphics.Clear(); visuals.Clear();
    }
}

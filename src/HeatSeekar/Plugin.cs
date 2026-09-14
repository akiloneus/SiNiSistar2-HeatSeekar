using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace HeatSeekar;

[BepInPlugin(Id, "SiNiSistar2 HeatSeekar", BuildVersion.Value)]
[BepInProcess("SiNiSistar2.exe")]
[BepInIncompatibility("AkiTool.SiNiMouseControl.IL2CPP_net6")]
[BepInIncompatibility("AkiTool.MouseInputMapper.IL2CPP_net6")]
[BepInIncompatibility("AkiTool.CursorTool.IL2CPP_net6")]
public sealed class Plugin : BasePlugin
{
    public const string Id = "AkiTool.SiNiSistar2.HeatSeekar";
    internal static Runtime? Runtime { get; private set; }
    private Harmony? harmony;
    private GameObject? host;

    public override void Load()
    {
        Runtime = new Runtime(PluginData.OpenSettings(Log), Log);
        harmony = new Harmony(Id);
        try
        {
            harmony.PatchAll(typeof(Plugin).Assembly);
            ClassInjector.RegisterTypeInIl2Cpp<HeatSeekarBehaviour>();
            host = new GameObject("HeatSeekar");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            host.AddComponent<HeatSeekarBehaviour>();
            Log.LogInfo("Native mouse rebinding, Aki-style menu navigation and sprite cursor installed. Configure HeatSeekar in the native Settings and Graphics pages.");
        }
        catch
        {
            harmony.UnpatchSelf();
            Runtime.Dispose();
            Runtime = null;
            throw;
        }
    }

    public override bool Unload()
    {
        harmony?.UnpatchSelf();
        Runtime?.Dispose();
        Runtime = null;
        if (host != null) UnityEngine.Object.Destroy(host);
        return true;
    }
}

public sealed class HeatSeekarBehaviour : MonoBehaviour
{
    public HeatSeekarBehaviour(IntPtr pointer) : base(pointer) { }
    public void LateUpdate() => Plugin.Runtime?.Tick();
    public void OnApplicationFocus(bool focused) => Plugin.Runtime?.FocusChanged(focused);
    public void OnApplicationPause(bool paused) => Plugin.Runtime?.FocusChanged(!paused);
    public void OnDisable() => Plugin.Runtime?.Suspend();
}

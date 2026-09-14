using System.Reflection;

namespace HeatSeekar;

internal static class ExternalUi
{
    private static Func<bool>? explorerOpen;
    private static Func<bool>? universeOpen;
    private static float nextResolve;
    public static bool IsOpen
    {
        get
        {
            if ((explorerOpen == null || universeOpen == null) && UnityEngine.Time.realtimeSinceStartup >= nextResolve)
            {
                nextResolve = UnityEngine.Time.realtimeSinceStartup + 2f;
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UnityExplorer.UI.UIManager")).FirstOrDefault(type => type != null);
                var getter = type?.GetProperty("ShowMenu", BindingFlags.Public | BindingFlags.Static)?.GetMethod;
                if (getter != null) explorerOpen = (Func<bool>)getter.CreateDelegate(typeof(Func<bool>));
                var universe = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("UniverseLib.UI.UniversalUI")).FirstOrDefault(type => type != null);
                var anyUi = universe?.GetProperty("AnyUIShowing", BindingFlags.Public | BindingFlags.Static)?.GetMethod;
                if (anyUi != null) universeOpen = (Func<bool>)anyUi.CreateDelegate(typeof(Func<bool>));
            }
            return (explorerOpen != null && explorerOpen()) || (universeOpen != null && universeOpen());
        }
    }
}

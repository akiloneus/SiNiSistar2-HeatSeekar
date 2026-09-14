using SiNiSistar2.Manager;

namespace HeatSeekar;

internal static class GameContext
{
    public static ManagerList? Managers
    {
        get
        {
            var instance = ManagerList.Instance;
            return instance != null && !instance.IsForbiddenManagerAccess ? instance : null;
        }
    }
}

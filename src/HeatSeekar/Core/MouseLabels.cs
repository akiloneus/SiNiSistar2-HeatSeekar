namespace HeatSeekar.Core;

internal static class MouseLabels
{
    public static string? FromPath(string? path)
    {
        if (path == "") return "--";
        if (string.IsNullOrEmpty(path)) return null;
        var normalized = path.ToLowerInvariant();
        if (!normalized.StartsWith("<mouse>/") && !normalized.StartsWith("/mouse/")) return null;
        return normalized[(normalized.IndexOf('/', 1) + 1)..] switch
        {
            "leftbutton" => "LMB",
            "rightbutton" => "RMB",
            "middlebutton" => "MMB",
            "backbutton" => "M4",
            "forwardbutton" => "M5",
            "scroll/up" => "MW+",
            "scroll/down" => "MW-",
            _ => null
        };
    }
}

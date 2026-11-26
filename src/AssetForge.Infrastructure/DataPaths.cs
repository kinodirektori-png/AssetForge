using Microsoft.Extensions.Configuration;

namespace AssetForge.Infrastructure;

public static class DataPaths
{
    /// <summary>
    /// Where Asset Forge keeps its data: <c>Storage:DataRoot</c> if set, otherwise the user's local app data. A relative
    /// <c>Storage:DataRoot</c> is resolved under the local app data folder, not the working directory, so the app finds
    /// the same data whichever folder it is started from.
    /// </summary>
    public static string Root(IConfiguration config)
    {
        string defaultRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssetForge");
        string? configured = config["Storage:DataRoot"];
        return string.IsNullOrWhiteSpace(configured)
            ? defaultRoot
            : Path.GetFullPath(configured, defaultRoot);
    }
}

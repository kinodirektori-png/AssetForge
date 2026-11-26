using Microsoft.Extensions.Configuration;

namespace AssetForge.Infrastructure;

public static class DataPaths
{
    /// <summary>Where Asset Forge keeps its data: <c>Storage:DataRoot</c> if set, otherwise the user's local app data.</summary>
    public static string Root(IConfiguration config)
    {
        string? configured = config["Storage:DataRoot"];
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AssetForge")
            : Path.GetFullPath(configured);
    }
}

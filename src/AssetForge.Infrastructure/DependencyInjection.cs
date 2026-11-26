using AssetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetForge.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// The database lives in the user profile, not in the repository or next to the app (NFR-6, NFR-7):
    /// <c>%LOCALAPPDATA%\AssetForge</c> on Windows. <c>Storage:DataRoot</c> or <c>ConnectionStrings:AssetForge</c> override it.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        string? connection = config.GetConnectionString("AssetForge");
        if (string.IsNullOrWhiteSpace(connection))
        {
            string dataRoot = DataPaths.Root(config);
            Directory.CreateDirectory(dataRoot);
            connection = $"Data Source={Path.Combine(dataRoot, "assetforge.db")}";
        }
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        return services;
    }
}

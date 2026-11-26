using AssetForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetForge.Infrastructure;

public static class DependencyInjection
{
    /// <param name="contentRoot">Relative data paths are resolved under this folder.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, string contentRoot)
    {
        string? connection = config.GetConnectionString("AssetForge");
        if (string.IsNullOrWhiteSpace(connection))
        {
            string dataDir = Path.Combine(contentRoot, "data");
            Directory.CreateDirectory(dataDir);
            connection = $"Data Source={Path.Combine(dataDir, "assetforge.db")}";
        }
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        return services;
    }
}

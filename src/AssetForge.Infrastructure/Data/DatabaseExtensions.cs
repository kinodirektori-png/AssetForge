using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AssetForge.Infrastructure.Data;

public static class DatabaseExtensions
{
    /// <summary>Applies pending migrations to the local database at startup.</summary>
    public static Task EnsureMigratedAsync(this DatabaseFacade database) => database.MigrateAsync();
}

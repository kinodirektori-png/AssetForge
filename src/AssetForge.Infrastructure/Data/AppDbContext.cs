using Microsoft.EntityFrameworkCore;

namespace AssetForge.Infrastructure.Data;

/// <summary>The local SQLite database. Entities and migrations are added by the feature issues (#5 onwards).</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);

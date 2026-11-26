using AssetForge.Application.Projects;
using Microsoft.EntityFrameworkCore;

namespace AssetForge.Infrastructure.Data;

/// <summary>The local SQLite database.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Every timestamp, nullable ones included, is stored as sortable text.</summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder b) =>
        b.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToUtcIsoStringConverter>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(Project.MaxNameLength);
            e.Property(p => p.NormalizedName).HasMaxLength(Project.MaxNameLength);
            e.HasIndex(p => p.NormalizedName).IsUnique();
            e.Property(p => p.Slug).HasMaxLength(Slugs.MaxLength);
            e.HasIndex(p => p.Slug).IsUnique();
            e.OwnsOne(p => p.StyleGuide, g =>
            {
                g.Property(x => x.Description).HasMaxLength(StyleGuide.MaxDescriptionLength);
                g.Property(x => x.Perspective).HasMaxLength(StyleGuide.MaxShortFieldLength);
                g.Property(x => x.Lighting).HasMaxLength(StyleGuide.MaxShortFieldLength);
                g.Property(x => x.Outline).HasMaxLength(StyleGuide.MaxShortFieldLength);
                g.Property(x => x.DetailLevel).HasMaxLength(StyleGuide.MaxShortFieldLength);
            });
            e.Navigation(p => p.StyleGuide).IsRequired();
        });
    }
}

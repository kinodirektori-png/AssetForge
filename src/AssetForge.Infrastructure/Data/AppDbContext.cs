using System.Globalization;
using AssetForge.Application.Projects;
using Microsoft.EntityFrameworkCore;

namespace AssetForge.Infrastructure.Data;

/// <summary>The local SQLite database.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

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
            // SQLite cannot order by DateTimeOffset, so store it as text in ISO 8601 form, which sorts correctly.
            e.Property(p => p.CreatedAt).HasConversion(
                v => v.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                v => DateTimeOffset.ParseExact(v, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
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

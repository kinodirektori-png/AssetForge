using System.Text;

namespace AssetForge.Application.Projects;

/// <summary>FR-1a. A project owns one style guide (FR-2).</summary>
public sealed class Project
{
    public const int MaxNameLength = 50;

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>The name in Unicode form C, trimmed and upper-cased, so names are unique ignoring case (AC-1.4).</summary>
    public required string NormalizedName { get; init; }

    /// <summary>Set once at creation and used in export file names (FR-1, FR-13).</summary>
    public required string Slug { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public StyleGuide StyleGuide { get; set; } = new();

    /// <summary>Makes a new project from a name already checked by <see cref="ProjectNameValidator"/>.</summary>
    public static Project Create(string name, string slug, DateTimeOffset now)
    {
        string clean = Clean(name);
        return new Project
        {
            Id = Guid.CreateVersion7(),
            Name = clean,
            NormalizedName = clean.ToUpperInvariant(),
            Slug = slug,
            CreatedAt = now,
        };
    }

    /// <summary>Unicode form C and trimmed, so names that look the same are stored the same (AC-1.4).</summary>
    public static string Clean(string? name) => name?.Normalize(NormalizationForm.FormC).Trim() ?? "";
}

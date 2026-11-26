namespace AssetForge.Application.Projects;

/// <summary>FR-1a. A project owns one style guide (FR-2).</summary>
public sealed class Project
{
    public const int MaxNameLength = 50;

    public Guid Id { get; init; }

    public required string Name { get; init; }

    /// <summary>The name trimmed and upper-cased, so names are unique ignoring case (AC-1.4).</summary>
    public required string NormalizedName { get; init; }

    /// <summary>Set once at creation and used in export file names (FR-1, FR-13).</summary>
    public required string Slug { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public StyleGuide StyleGuide { get; set; } = new();

    public static string Normalize(string name) => name.Trim().ToUpperInvariant();
}

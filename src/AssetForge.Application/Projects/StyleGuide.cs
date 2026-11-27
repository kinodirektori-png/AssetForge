namespace AssetForge.Application.Projects;

/// <summary>FR-2 style guide. Every field may be empty (DEC-6). Reference images are out of scope (spec §2).</summary>
public sealed class StyleGuide
{
    public const int MaxDescriptionLength = 300;
    public const int MaxShortFieldLength = 100;
    public const int MaxPaletteColours = 16;
    public const int MaxAvoidTerms = 20;
    public const int MaxAvoidTermLength = 40;

    public string? Description { get; init; }

    public string? Perspective { get; init; }

    /// <summary>Hex colours as <c>#RRGGBB</c>, upper case.</summary>
    public List<string> Palette { get; init; } = [];

    public string? Lighting { get; init; }

    public string? Outline { get; init; }

    public string? DetailLevel { get; init; }

    public List<string> Avoid { get; init; } = [];

    public DateTimeOffset? UpdatedAt { get; init; }
}

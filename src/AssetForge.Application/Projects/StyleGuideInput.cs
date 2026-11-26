namespace AssetForge.Application.Projects;

/// <summary>Input for saving a style guide. Missing fields and lists are saved as empty (DEC-6).</summary>
public sealed record StyleGuideInput(
    string? Description = null,
    string? Perspective = null,
    IReadOnlyList<string?>? Palette = null,
    string? Lighting = null,
    string? Outline = null,
    string? DetailLevel = null,
    IReadOnlyList<string?>? Avoid = null);

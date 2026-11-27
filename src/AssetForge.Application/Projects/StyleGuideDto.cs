namespace AssetForge.Application.Projects;

public sealed record StyleGuideDto(
    string? Description,
    string? Perspective,
    IReadOnlyList<string> Palette,
    string? Lighting,
    string? Outline,
    string? DetailLevel,
    IReadOnlyList<string> Avoid,
    DateTimeOffset? UpdatedAt);

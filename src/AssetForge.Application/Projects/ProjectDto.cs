namespace AssetForge.Application.Projects;

public sealed record ProjectDto(Guid Id, string Name, string Slug, DateTimeOffset CreatedAt);

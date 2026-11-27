namespace AssetForge.Application.Projects;

/// <summary>Input for creating a project. The name is checked by <see cref="ProjectService"/>.</summary>
public sealed record CreateProjectInput(string? Name);

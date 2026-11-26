namespace AssetForge.Application.Projects;

/// <summary>Persistence for projects. Implemented with EF Core in Infrastructure.</summary>
public interface IProjectStore
{
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct);

    Task<Project?> FindAsync(Guid id, CancellationToken ct);

    Task<bool> NameExistsAsync(string normalizedName, CancellationToken ct);

    Task<IReadOnlyList<string>> SlugsStartingWithAsync(string prefix, CancellationToken ct);

    /// <summary>Adds the project. Throws <see cref="ConflictException"/> if its name is already taken.</summary>
    Task AddAsync(Project project, CancellationToken ct);

    Task SaveStyleGuideAsync(Project project, StyleGuide styleGuide, CancellationToken ct);
}

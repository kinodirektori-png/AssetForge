namespace AssetForge.Application.Projects;

/// <summary>Persistence for projects. Implemented with EF Core in Infrastructure.</summary>
public interface IProjectStore
{
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct);

    /// <summary>Reads a project without tracking it. Changes to the result are not saved.</summary>
    Task<Project?> FindAsync(Guid id, CancellationToken ct);

    Task<bool> NameExistsAsync(string normalizedName, CancellationToken ct);

    Task<IReadOnlyList<string>> SlugsStartingWithAsync(string prefix, CancellationToken ct);

    /// <summary>Adds the project. Throws <see cref="ConflictException"/> if its name is already taken.</summary>
    Task AddAsync(Project project, CancellationToken ct);

    /// <summary>Replaces the style guide of a project. Returns <c>false</c> if there is no such project.</summary>
    Task<bool> SaveStyleGuideAsync(Guid projectId, StyleGuide styleGuide, CancellationToken ct);
}

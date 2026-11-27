namespace AssetForge.Application.Projects;

/// <summary>FR-1a projects and the FR-2 style guide.</summary>
public sealed class ProjectService(IProjectStore store, TimeProvider clock)
{
    public async Task<ProjectDto> CreateAsync(string? name, CancellationToken ct)
    {
        string clean = ProjectNameValidator.Validate(name);
        if (await store.NameExistsAsync(Project.Normalize(clean), ct))
            throw new ConflictException($"A project named \"{clean}\" already exists.");

        string slug = Slugs.From(clean, "project");
        slug = Slugs.MakeUnique(slug, (await store.SlugsStartingWithAsync(Slugs.SharedPrefix(slug), ct)).ToHashSet());

        var project = Project.Create(clean, slug, clock.GetUtcNow());
        await store.AddAsync(project, ct);
        return ToDto(project);
    }

    public async Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken ct) =>
        [.. (await store.ListAsync(ct)).Select(ToDto)];

    public async Task<ProjectDto> GetAsync(Guid id, CancellationToken ct) => ToDto(await FindAsync(id, ct));

    public async Task<StyleGuideDto> GetStyleGuideAsync(Guid projectId, CancellationToken ct) =>
        ToDto((await FindAsync(projectId, ct)).StyleGuide);

    public async Task<StyleGuideDto> SaveStyleGuideAsync(Guid projectId, StyleGuideInput input, CancellationToken ct)
    {
        var guide = StyleGuideValidator.Validate(input, clock.GetUtcNow());
        await store.SaveStyleGuideAsync(projectId, guide, ct);
        return ToDto(guide);
    }

    private async Task<Project> FindAsync(Guid id, CancellationToken ct) =>
        await store.FindAsync(id, ct) ?? throw NotFound();

    private static NotFoundException NotFound() => new("The project");

    private static ProjectDto ToDto(Project p) => new(p.Id, p.Name, p.Slug, p.CreatedAt);

    // The lists are copied so the DTO cannot be used to change the entity.
    private static StyleGuideDto ToDto(StyleGuide g) =>
        new(g.Description, g.Perspective, [.. g.Palette], g.Lighting, g.Outline, g.DetailLevel, [.. g.Avoid], g.UpdatedAt);
}

using AssetForge.Application;
using AssetForge.Application.Projects;
using AssetForge.Tests.Support;

namespace AssetForge.Tests.Unit;

/// <summary>How the service maps store results to errors, with a fake store.</summary>
public sealed class ProjectServiceTests
{
    private sealed class FakeStore : IProjectStore
    {
        public HashSet<string> Slugs { get; init; } = [];
        public bool SaveResult { get; init; } = true;
        public bool NameExists { get; init; }
        public AddResult AddResult { get; init; } = AddResult.Added;

        public Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Project>>([]);
        public Task<Project?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult<Project?>(null);
        public Task<bool> NameExistsAsync(string normalizedName, CancellationToken ct) => Task.FromResult(NameExists);
        public Task<IReadOnlyList<string>> SlugsStartingWithAsync(string prefix, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<string>>([.. Slugs]);
        public Task<AddResult> AddAsync(Project project, CancellationToken ct) => Task.FromResult(AddResult);
        public Task<bool> SaveStyleGuideAsync(Guid projectId, StyleGuide styleGuide, CancellationToken ct) => Task.FromResult(SaveResult);
    }

    [Fact]
    public async Task Running_out_of_slug_numbers_is_a_conflict()
    {
        var taken = Enumerable.Range(2, 9998).Select(n => $"dungeon-{n}").Append("dungeon").ToHashSet();
        var service = new ProjectService(new FakeStore { Slugs = taken }, new SteppingTimeProvider());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync("Dungeon", CancellationToken.None));
    }

    [Fact]
    public async Task Saving_a_style_guide_the_store_could_not_place_is_not_found()
    {
        var service = new ProjectService(new FakeStore { SaveResult = false }, new SteppingTimeProvider());

        await Assert.ThrowsAsync<NotFoundException>(() => service.SaveStyleGuideAsync(Guid.NewGuid(), new StyleGuideInput(), CancellationToken.None));
    }

    [Fact]
    public async Task A_name_already_taken_is_a_conflict_before_any_slug_work()
    {
        var service = new ProjectService(new FakeStore { NameExists = true }, new SteppingTimeProvider());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync("Dungeon", CancellationToken.None));
        Assert.Contains("already exists", ex.Message);
    }

    [Theory]
    [InlineData(AddResult.NameTaken, "already exists")]
    [InlineData(AddResult.SlugTaken, "Try again")]
    public async Task A_clash_found_when_adding_gets_the_matching_message(AddResult result, string expected)
    {
        var service = new ProjectService(new FakeStore { AddResult = result }, new SteppingTimeProvider());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync("Dungeon", CancellationToken.None));
        Assert.Contains(expected, ex.Message);
    }
}

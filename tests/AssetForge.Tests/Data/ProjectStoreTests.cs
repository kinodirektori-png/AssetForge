using AssetForge.Application;
using AssetForge.Application.Projects;
using AssetForge.Infrastructure.Data;
using AssetForge.Tests.Support;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetForge.Tests.Data;

/// <summary>The EF Core store and the SQLite database behind it (spec §6, issue #5).</summary>
public sealed class ProjectStoreTests : IDisposable
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Project NewProject(string name, string slug) => Project.Create(name, slug, DateTimeOffset.UnixEpoch);

    private async Task<int> ProjectCountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Projects.CountAsync();
    }

    [Fact]
    public async Task A_racing_slug_clash_is_a_conflict_that_does_not_blame_the_name()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();
        await store.AddAsync(NewProject("Dungeon!", "dungeon"), Ct);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => store.AddAsync(NewProject("Dungeon?", "dungeon"), Ct));

        Assert.Contains("Try again", ex.Message);
    }

    [Fact]
    public async Task The_unique_index_turns_a_racing_duplicate_name_into_a_conflict()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();
        await store.AddAsync(NewProject("Dungeon", "dungeon"), Ct);

        // Bypasses the service's pre-check, as a second request created at the same moment would.
        var ex = await Assert.ThrowsAsync<ConflictException>(() => store.AddAsync(NewProject("dungeon", "dungeon-x"), Ct));

        Assert.Contains("already exists", ex.Message);
        Assert.Equal(1, await ProjectCountAsync());
    }

    [Fact]
    public async Task A_rejected_write_that_is_not_a_duplicate_is_not_reported_as_a_conflict()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();

        // A NOT NULL failure is also SQLITE_CONSTRAINT (19), but not a unique-index clash (2067).
        var broken = new Project { Id = Guid.CreateVersion7(), Name = null!, NormalizedName = "BROKEN", Slug = "broken", CreatedAt = DateTimeOffset.UnixEpoch };

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => store.AddAsync(broken, Ct));
        Assert.Equal(19, Assert.IsType<SqliteException>(ex.InnerException).SqliteErrorCode);
        Assert.Equal(0, await ProjectCountAsync());
    }

    [Fact]
    public async Task Saving_a_style_guide_stores_it_for_that_project_only()
    {
        Guid first, second;
        using (var scope = _factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();
            var a = NewProject("First", "first");
            var b = NewProject("Second", "second");
            await store.AddAsync(a, Ct);
            await store.AddAsync(b, Ct);
            (first, second) = (a.Id, b.Id);
            await store.SaveStyleGuideAsync(first, new StyleGuide { Description = "pixel art", Palette = ["#112233"] }, Ct);
        }

        using var read = _factory.Services.CreateScope();
        var reader = read.ServiceProvider.GetRequiredService<IProjectStore>();
        var saved = (await reader.FindAsync(first, Ct))!.StyleGuide;
        Assert.Equal("pixel art", saved.Description);
        Assert.Equal(["#112233"], saved.Palette);
        Assert.Null((await reader.FindAsync(second, Ct))!.StyleGuide.Description);
    }

    [Fact]
    public async Task Saving_a_style_guide_for_an_unknown_project_is_reported_as_not_saved()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();

        Assert.False(await store.SaveStyleGuideAsync(Guid.NewGuid(), new StyleGuide(), Ct));
    }

    [Fact]
    public async Task Style_guide_save_time_can_be_sorted_and_filtered_in_the_database()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IProjectStore>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var first = NewProject("First", "first");
        var second = NewProject("Second", "second");
        await store.AddAsync(first, Ct);
        await store.AddAsync(second, Ct);
        var start = SteppingTimeProvider.Start;
        await store.SaveStyleGuideAsync(second.Id, new StyleGuide { UpdatedAt = start.AddMinutes(1) }, Ct);
        await store.SaveStyleGuideAsync(first.Id, new StyleGuide { UpdatedAt = start.AddMinutes(2) }, Ct);

        var names = await db.Projects
            .Where(p => p.StyleGuide.UpdatedAt > start)
            .OrderBy(p => p.StyleGuide.UpdatedAt)
            .Select(p => p.Name)
            .ToListAsync();

        Assert.Equal(["Second", "First"], names);
    }

    [Fact]
    public async Task A_timestamp_written_in_the_earlier_format_can_still_be_read()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.CreateVersion7();
        // The first version stored DateTimeOffset's default text, with a local offset and a space.
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO Projects (Id, Name, NormalizedName, Slug, CreatedAt, StyleGuide_Palette, StyleGuide_Avoid) VALUES ({0}, 'Old', 'OLD', 'old', '2026-11-26 21:10:26.1234567+02:00', '[]', '[]')",
            id.ToString().ToUpperInvariant());

        var project = await db.Projects.AsNoTracking().SingleAsync(p => p.Id == id);

        Assert.Equal(new DateTimeOffset(2026, 11, 26, 19, 10, 26, TimeSpan.Zero).AddTicks(1234567), project.CreatedAt.ToUniversalTime());
    }
}

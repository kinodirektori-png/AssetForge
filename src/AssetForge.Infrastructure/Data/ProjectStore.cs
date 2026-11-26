using AssetForge.Application;
using AssetForge.Application.Projects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AssetForge.Infrastructure.Data;

public sealed class ProjectStore(AppDbContext db) : IProjectStore
{
    /// <summary>SQLITE_CONSTRAINT_UNIQUE: a unique index refused the row. Other constraint failures are not conflicts.</summary>
    private const int UniqueConstraintFailed = 2067;

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct) =>
        await db.Projects.AsNoTracking().OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).ToListAsync(ct);

    public Task<Project?> FindAsync(Guid id, CancellationToken ct) =>
        db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Project?> FindForUpdateAsync(Guid id, CancellationToken ct) =>
        db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> NameExistsAsync(string normalizedName, CancellationToken ct) =>
        db.Projects.AnyAsync(p => p.NormalizedName == normalizedName, ct);

    public async Task<IReadOnlyList<string>> SlugsStartingWithAsync(string prefix, CancellationToken ct) =>
        await db.Projects.Where(p => p.Slug.StartsWith(prefix)).Select(p => p.Slug).ToListAsync(ct);

    public async Task AddAsync(Project project, CancellationToken ct)
    {
        db.Projects.Add(project);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: UniqueConstraintFailed })
        {
            // A project created at the same moment took the name or the slug. Ask the database which one,
            // instead of reading SQLite's error text.
            db.Entry(project).State = EntityState.Detached;
            throw new ConflictException(await NameExistsAsync(project.NormalizedName, ct)
                ? $"A project named \"{project.Name}\" already exists."
                : "Another project was created at the same moment. Try again.");
        }
    }

    public async Task SaveStyleGuideAsync(Project project, StyleGuide styleGuide, CancellationToken ct)
    {
        project.StyleGuide = styleGuide;
        await db.SaveChangesAsync(ct);
    }
}

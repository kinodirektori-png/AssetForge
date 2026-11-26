using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AssetForge.Application.Projects;

/// <summary>FR-1a projects and the FR-2 style guide.</summary>
public sealed partial class ProjectService(IProjectStore store, TimeProvider clock)
{
    public async Task<ProjectDto> CreateAsync(string? name, CancellationToken ct)
    {
        // NFC first, so names that look the same are stored the same and compare equal (AC-1.4).
        string trimmed = name?.Normalize(NormalizationForm.FormC).Trim() ?? "";
        var errors = new Errors();
        if (trimmed.Length == 0)
            errors.Add("name", "Name is required.");
        // FR-1 counts characters as people see them, so an emoji or a letter with an accent counts as one.
        else if (new StringInfo(trimmed).LengthInTextElements > Project.MaxNameLength)
            errors.Add("name", $"Name must be at most {Project.MaxNameLength} characters.");
        else if (trimmed.Any(IsHiddenCharacter))
            errors.Add("name", "Name must not contain control or invisible formatting characters.");
        errors.ThrowIfAny();

        string normalized = Project.Normalize(trimmed);
        if (await store.NameExistsAsync(normalized, ct))
            throw new ConflictException($"A project named \"{trimmed}\" already exists.");

        string slug = Slugs.From(trimmed, "project");
        slug = Slugs.MakeUnique(slug, [.. await store.SlugsStartingWithAsync(Slugs.SharedPrefix(slug), ct)]);

        var project = new Project
        {
            Id = Guid.CreateVersion7(),
            Name = trimmed,
            NormalizedName = normalized,
            Slug = slug,
            CreatedAt = clock.GetUtcNow(),
        };
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
        var guide = Validate(input, clock.GetUtcNow());
        var project = await store.FindForUpdateAsync(projectId, ct) ?? throw NotFound();
        await store.SaveStyleGuideAsync(project, guide, ct);
        return ToDto(guide);
    }

    /// <summary>Checks the FR-2 limits (AC-2.2, AC-2.3, AC-2.5) and returns the cleaned style guide.</summary>
    private static StyleGuide Validate(StyleGuideInput input, DateTimeOffset now)
    {
        var errors = new Errors();
        string? description = Text(input.Description, "description", StyleGuide.MaxDescriptionLength, errors);
        string? perspective = Text(input.Perspective, "perspective", StyleGuide.MaxShortFieldLength, errors);
        string? lighting = Text(input.Lighting, "lighting", StyleGuide.MaxShortFieldLength, errors);
        string? outline = Text(input.Outline, "outline", StyleGuide.MaxShortFieldLength, errors);
        string? detailLevel = Text(input.DetailLevel, "detailLevel", StyleGuide.MaxShortFieldLength, errors);

        var palette = new List<string>();
        var paletteIn = input.Palette ?? [];
        if (paletteIn.Count > StyleGuide.MaxPaletteColours)
            errors.Add("palette", $"At most {StyleGuide.MaxPaletteColours} colours are allowed.");
        for (int i = 0; i < paletteIn.Count && i < StyleGuide.MaxPaletteColours; i++)
        {
            string value = paletteIn[i]?.Trim() ?? "";
            if (HexColour().IsMatch(value))
                palette.Add(value.ToUpperInvariant());
            else
                errors.Add($"palette[{i}]", $"\"{Shorten(value)}\" is not a hex colour like #A1B2C3.");
        }

        // Blank terms are dropped; error keys use the index the caller sent.
        var avoid = new List<string>();
        var avoidIn = input.Avoid ?? [];
        for (int i = 0; i < avoidIn.Count; i++)
        {
            string term = avoidIn[i]?.Trim() ?? "";
            if (term.Length > StyleGuide.MaxAvoidTermLength)
                errors.Add($"avoid[{i}]", $"Each term must be at most {StyleGuide.MaxAvoidTermLength} characters.");
            else if (term.Length > 0)
                avoid.Add(term);
        }
        if (avoidIn.Count(t => !string.IsNullOrWhiteSpace(t)) > StyleGuide.MaxAvoidTerms)
            errors.Add("avoid", $"At most {StyleGuide.MaxAvoidTerms} terms are allowed.");

        errors.ThrowIfAny();
        return new StyleGuide
        {
            Description = description,
            Perspective = perspective,
            Palette = palette,
            Lighting = lighting,
            Outline = outline,
            DetailLevel = detailLevel,
            Avoid = avoid,
            UpdatedAt = now,
        };
    }

    private static string? Text(string? value, string field, int max, Errors errors)
    {
        string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (trimmed is { Length: var length } && length > max)
            errors.Add(field, $"{field} must be at most {max} characters.");
        return trimmed;
    }

    /// <summary>Control and invisible formatting characters, except the zero-width joiners some scripts and emoji need.</summary>
    private static bool IsHiddenCharacter(char c) =>
        c is not ('\u200C' or '\u200D')
        && CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.Format;

    private static string Shorten(string value) => value.Length <= 20 ? value : value[..20] + "…";

    private async Task<Project> FindAsync(Guid id, CancellationToken ct) =>
        await store.FindAsync(id, ct) ?? throw NotFound();

    private static NotFoundException NotFound() => new("The project");

    private static ProjectDto ToDto(Project p) => new(p.Id, p.Name, p.Slug, p.CreatedAt);

    // The lists are copied so the DTO cannot be used to change the entity.
    private static StyleGuideDto ToDto(StyleGuide g) =>
        new(g.Description, g.Perspective, [.. g.Palette], g.Lighting, g.Outline, g.DetailLevel, [.. g.Avoid], g.UpdatedAt);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColour();

    private sealed class Errors
    {
        private readonly Dictionary<string, List<string>> _errors = [];

        public void Add(string field, string message)
        {
            if (!_errors.TryGetValue(field, out var list))
                _errors[field] = list = [];
            list.Add(message);
        }

        public void ThrowIfAny()
        {
            if (_errors.Count > 0)
                throw new ValidationFailedException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}

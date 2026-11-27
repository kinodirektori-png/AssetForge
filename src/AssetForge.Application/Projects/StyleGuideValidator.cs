using System.Text.RegularExpressions;

namespace AssetForge.Application.Projects;

/// <summary>Checks the FR-2 limits (AC-2.2, AC-2.3, AC-2.5) and returns the cleaned style guide.</summary>
public static partial class StyleGuideValidator
{
    public static StyleGuide Validate(StyleGuideInput input, DateTimeOffset now)
    {
        var errors = new ValidationErrors();
        string? description = Text(input.Description, nameof(StyleGuideInput.Description), StyleGuide.MaxDescriptionLength, errors);
        string? perspective = Text(input.Perspective, nameof(StyleGuideInput.Perspective), StyleGuide.MaxShortFieldLength, errors);
        string? lighting = Text(input.Lighting, nameof(StyleGuideInput.Lighting), StyleGuide.MaxShortFieldLength, errors);
        string? outline = Text(input.Outline, nameof(StyleGuideInput.Outline), StyleGuide.MaxShortFieldLength, errors);
        string? detailLevel = Text(input.DetailLevel, nameof(StyleGuideInput.DetailLevel), StyleGuide.MaxShortFieldLength, errors);
        var palette = Palette(input.Palette ?? [], errors);
        var avoid = Avoid(input.Avoid ?? [], errors);

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

    private static List<string> Palette(IReadOnlyList<string?> input, ValidationErrors errors)
    {
        string key = nameof(StyleGuideInput.Palette);
        if (input.Count > StyleGuide.MaxPaletteColours)
            errors.Add(key, $"At most {StyleGuide.MaxPaletteColours} colours are allowed.");

        var palette = new List<string>();
        for (int i = 0; i < input.Count && i < StyleGuide.MaxPaletteColours; i++)
        {
            string value = input[i]?.Trim() ?? "";
            if (HexColour().IsMatch(value))
                palette.Add(value.ToUpperInvariant());
            else
                errors.Add($"{key}[{i}]", $"\"{TextLength.Shorten(value)}\" is not a hex colour like #A1B2C3.");
        }
        return palette;
    }

    /// <summary>Blank terms are dropped; error keys use the index the caller sent.</summary>
    private static List<string> Avoid(IReadOnlyList<string?> input, ValidationErrors errors)
    {
        string key = nameof(StyleGuideInput.Avoid);
        var avoid = new List<string>();
        for (int i = 0; i < input.Count; i++)
        {
            string term = input[i]?.Trim() ?? "";
            if (errors.TryAddLengthError($"{key}[{i}]", "Each term", term, StyleGuide.MaxAvoidTermLength))
                continue;
            if (term.Length > 0)
                avoid.Add(term);
        }
        if (input.Count(t => !string.IsNullOrWhiteSpace(t)) > StyleGuide.MaxAvoidTerms)
            errors.Add(key, $"At most {StyleGuide.MaxAvoidTerms} terms are allowed.");
        return avoid;
    }

    private static string? Text(string? value, string property, int max, ValidationErrors errors)
    {
        string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (trimmed is not null)
            errors.TryAddLengthError(property, ValidationErrors.Label(property), trimmed, max);
        return trimmed;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColour();
}

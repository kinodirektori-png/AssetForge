using System.Globalization;

namespace AssetForge.Application.Projects;

/// <summary>The FR-1 rules for a project name (AC-1.4 is checked against the store, not here).</summary>
public static class ProjectNameValidator
{
    /// <summary>Returns the cleaned name, or throws <see cref="ValidationFailedException"/>.</summary>
    public static string Validate(string? name)
    {
        string clean = Project.Clean(name);
        string key = nameof(CreateProjectInput.Name);
        var errors = new ValidationErrors();
        if (clean.Length == 0)
            errors.Add(key, "Name is required.");
        else if (!errors.TryAddLengthError(key, "Name", clean, Project.MaxNameLength))
        {
            if (clean.Any(IsHiddenCharacter))
                errors.Add(key, "Name must not contain control or invisible formatting characters.");
        }
        errors.ThrowIfAny();
        return clean;
    }

    /// <summary>Control and invisible formatting characters, except the zero-width joiners some scripts and emoji need.</summary>
    private static bool IsHiddenCharacter(char c) =>
        c is not ('\u200C' or '\u200D')
        && CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.Format;
}

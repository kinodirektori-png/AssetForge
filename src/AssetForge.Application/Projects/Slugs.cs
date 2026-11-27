using System.Globalization;
using System.Text;

namespace AssetForge.Application.Projects;

/// <summary>Builds the short identifier used in export file names: lower-case letters, digits and single hyphens.</summary>
public static class Slugs
{
    public const int MaxLength = 30;

    /// <summary>The longest suffix <see cref="MakeUnique"/> adds: a hyphen and four digits (<c>-9999</c>).</summary>
    public const int MaxSuffixLength = 5;

    private const int MaxNumber = 9999;

    public static string From(string text, string fallback)
    {
        var sb = new StringBuilder();
        foreach (char c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            char lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9')
                sb.Append(lower);
            else if (sb.Length > 0 && sb[^1] != '-')
                sb.Append('-');
        }
        string slug = sb.ToString().Trim('-');
        if (slug.Length > MaxLength)
            slug = slug[..MaxLength].TrimEnd('-');
        return slug.Length == 0 ? fallback : slug;
    }

    /// <summary>
    /// The part every numbered variant of <paramref name="slug"/> starts with: <see cref="MakeUnique"/> may shorten a long
    /// slug, and drop a hyphen left at its end, to make room for a suffix of up to <see cref="MaxSuffixLength"/> characters.
    /// </summary>
    public static string SharedPrefix(string slug) =>
        slug.Length <= MaxLength - MaxSuffixLength ? slug : slug[..(MaxLength - MaxSuffixLength)].TrimEnd('-');

    /// <summary>
    /// Appends -2, -3 ... until the slug is not in <paramref name="taken"/>. Gives up with a
    /// <see cref="ConflictException"/> after <c>-9999</c>, so the loop always ends.
    /// </summary>
    public static string MakeUnique(string slug, IReadOnlySet<string> taken)
    {
        if (!taken.Contains(slug))
            return slug;
        for (int n = 2; n <= MaxNumber; n++)
        {
            string suffix = "-" + n.ToString(CultureInfo.InvariantCulture);
            string candidate = (slug.Length + suffix.Length > MaxLength ? slug[..(MaxLength - suffix.Length)].TrimEnd('-') : slug) + suffix;
            if (!taken.Contains(candidate))
                return candidate;
        }
        throw new ConflictException("Too many projects have a name like this one. Choose a different name.");
    }
}

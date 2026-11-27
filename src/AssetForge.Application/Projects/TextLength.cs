using System.Globalization;

namespace AssetForge.Application.Projects;

/// <summary>
/// How long a text is, as a person counts it (FR-1, FR-2): an emoji or a letter with an accent is one character.
/// One rule for every field, so the name, the style guide and the error messages agree.
/// </summary>
public static class TextLength
{
    /// <summary>
    /// Room in UTF-16 units for each allowed character. The longest real sequences are about 19 (a family with skin
    /// tones); 16 per character keeps ordinary text far under the cap and still bounds a value to a few thousand units.
    /// </summary>
    private const int MaxUnitsPerCharacter = 16;

    private const int ShortenedLength = 20;

    /// <summary>
    /// Stacked combining marks also count as one character, so the size in UTF-16 units is capped as well, or one
    /// "character" could be megabytes.
    /// </summary>
    public static bool Exceeds(string value, int maxCharacters) =>
        value.Length > maxCharacters * MaxUnitsPerCharacter || new StringInfo(value).LengthInTextElements > maxCharacters;

    /// <summary>Cuts a long value for an error message, on whole characters, never through a surrogate pair or a letter and its accent.</summary>
    public static string Shorten(string value)
    {
        var info = new StringInfo(value);
        return info.LengthInTextElements <= ShortenedLength ? value : info.SubstringByTextElements(0, ShortenedLength) + "…";
    }
}

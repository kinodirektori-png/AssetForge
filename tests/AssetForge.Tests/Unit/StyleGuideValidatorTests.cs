using System.Text.Json;
using AssetForge.Application;
using AssetForge.Application.Projects;

namespace AssetForge.Tests.Unit;

/// <summary>The FR-2 rules, without a host or a database.</summary>
public sealed class StyleGuideValidatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;

    private static StyleGuide Valid(StyleGuideInput input) => StyleGuideValidator.Validate(input, Now);

    private static IDictionary<string, string[]> Errors(StyleGuideInput input) =>
        Assert.Throws<ValidationFailedException>(() => StyleGuideValidator.Validate(input, Now)).Errors;

    [Fact]
    public void Error_keys_match_the_json_property_names_and_messages_use_words()
    {
        var input = new StyleGuideInput(
            Description: new string('x', 301), Perspective: new string('x', 101), Lighting: new string('x', 101),
            Outline: new string('x', 101), DetailLevel: new string('x', 101));

        var errors = Errors(input);

        var jsonNames = typeof(StyleGuideInput).GetProperties()
            .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
            .Where(n => n is not ("palette" or "avoid"));
        Assert.Equal(jsonNames.Order(), errors.Keys.Order());
        Assert.Equal("Detail level must be at most 100 characters.", Assert.Single(errors["detailLevel"]));
    }

    [Fact]
    public void Values_are_trimmed_blanks_become_empty_and_colours_upper_case()
    {
        var guide = Valid(new StyleGuideInput(
            Description: "   ", Lighting: "  top-left  ", Palette: [" #abcdef "], Avoid: [null, "  ", "text", " blur "]));

        Assert.Null(guide.Description);
        Assert.Equal("top-left", guide.Lighting);
        Assert.Equal(["#ABCDEF"], guide.Palette);
        Assert.Equal(["text", "blur"], guide.Avoid);
        Assert.Equal(Now, guide.UpdatedAt);
    }

    [Fact]
    public void Missing_lists_are_saved_as_empty()
    {
        var guide = Valid(new StyleGuideInput());

        Assert.Empty(guide.Palette);
        Assert.Empty(guide.Avoid);
    }

    [Fact]
    public void Sixteen_colours_are_accepted_and_seventeen_get_one_error()
    {
        Assert.Equal(16, Valid(new StyleGuideInput(Palette: [.. Enumerable.Range(0, 16).Select(i => $"#0000{i:X2}")])).Palette.Count);

        var errors = Errors(new StyleGuideInput(Palette: [.. Enumerable.Repeat("bad", 1000)]));

        Assert.True(errors.Count <= StyleGuide.MaxPaletteColours + 1, $"{errors.Count} errors");
        Assert.Contains("palette", errors.Keys);
    }

    [Fact]
    public void Palette_and_avoid_errors_name_the_index_sent()
    {
        var errors = Errors(new StyleGuideInput(Palette: ["#112233", "nope", null], Avoid: ["ok", new string('y', 41)]));

        Assert.Equal(["avoid[1]", "palette[1]", "palette[2]"], errors.Keys.Order());
    }

    [Fact]
    public void Avoid_terms_are_limited_in_count_and_length()
    {
        Assert.Contains("20", Assert.Single(Errors(new StyleGuideInput(Avoid: [.. Enumerable.Range(0, 21).Select(i => $"term{i}")]))["avoid"]));
        Assert.Contains("40", Assert.Single(Errors(new StyleGuideInput(Avoid: ["ok", new string('x', 41)]))["avoid[1]"]));
        Assert.Equal(20, Valid(new StyleGuideInput(Avoid: [.. Enumerable.Repeat(new string('x', 40), 20)])).Avoid.Count);
    }

    [Fact]
    public void Limits_count_characters_a_person_sees()
    {
        string smiles300 = string.Concat(Enumerable.Repeat("\U0001F600", 300));

        Assert.Equal(smiles300, Valid(new StyleGuideInput(Description: smiles300)).Description);
        Assert.Contains("300", Assert.Single(Errors(new StyleGuideInput(Description: smiles300 + "\U0001F600"))["description"]));
    }

    [Fact]
    public void A_huge_value_of_stacked_marks_is_too_large_in_every_field()
    {
        string huge = "a" + new string('\u0300', 200_000);

        var errors = Errors(new StyleGuideInput(Description: huge, Lighting: huge, Avoid: [huge]));

        Assert.Equal(["avoid[0]", "description", "lighting"], errors.Keys.Order());
        Assert.Equal("Description is too large.", Assert.Single(errors["description"]));
        Assert.Equal("Each term is too large.", Assert.Single(errors["avoid[0]"]));
    }

    [Fact]
    public void A_long_invalid_colour_is_cut_on_whole_characters_in_the_error()
    {
        string bad = "x" + string.Concat(Enumerable.Repeat("\U0001F600", 30));   // an odd first unit shifts the cut

        string message = Assert.Single(Errors(new StyleGuideInput(Palette: [bad]))["palette[0]"]);

        Assert.Contains("x" + string.Concat(Enumerable.Repeat("\U0001F600", 19)) + "…", message);
        Assert.DoesNotContain('�', message);
    }
}

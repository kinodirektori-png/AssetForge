using AssetForge.Application;
using AssetForge.Application.Projects;

namespace AssetForge.Tests.Unit;

/// <summary>The FR-1 and FR-2 rules, without a host or a database.</summary>
public sealed class ValidatorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch;

    private static IDictionary<string, string[]> Errors(StyleGuideInput input) =>
        Assert.Throws<ValidationFailedException>(() => StyleGuideValidator.Validate(input, Now)).Errors;

    [Fact]
    public void A_name_is_trimmed_and_put_in_unicode_form_C()
    {
        Assert.Equal("Café", ProjectNameValidator.Validate("  Cafe\u0301 "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string? name)
    {
        var ex = Assert.Throws<ValidationFailedException>(() => ProjectNameValidator.Validate(name));
        Assert.Contains("name", ex.Errors.Keys);
    }

    [Fact]
    public void Project_normalize_trims_composes_and_upper_cases()
    {
        Assert.Equal("CAFÉ", Project.Normalize(" cafe\u0301 "));
        Assert.Equal(Project.Normalize("Dungeon"), Project.Normalize(" dungeon"));
    }

    [Fact]
    public void A_new_project_gets_cleaned_names_and_a_version_7_id()
    {
        var project = Project.Create(" Dungeon ", "dungeon", Now);

        Assert.Equal("Dungeon", project.Name);
        Assert.Equal("DUNGEON", project.NormalizedName);
        Assert.Equal(7, project.Id.Version);
        Assert.Equal(Now, project.CreatedAt);
    }

    [Fact]
    public void Error_keys_are_the_json_names_and_messages_use_words()
    {
        var errors = Errors(new StyleGuideInput(DetailLevel: new string('x', 101)));

        Assert.Equal("Detail level must be at most 100 characters.", Assert.Single(errors["detailLevel"]));
    }

    [Fact]
    public void Colours_are_upper_cased_and_blank_terms_dropped()
    {
        var guide = StyleGuideValidator.Validate(new StyleGuideInput(Palette: [" #aabbcc "], Avoid: ["", " text ", "  "]), Now);

        Assert.Equal(["#AABBCC"], guide.Palette);
        Assert.Equal(["text"], guide.Avoid);
        Assert.Equal(Now, guide.UpdatedAt);
    }

    [Fact]
    public void Palette_and_avoid_errors_name_the_index_sent()
    {
        var errors = Errors(new StyleGuideInput(Palette: ["#112233", "nope"], Avoid: ["ok", new string('y', 41)]));

        Assert.Contains("palette[1]", errors.Keys);
        Assert.Contains("avoid[1]", errors.Keys);
    }

    [Fact]
    public void Counting_uses_characters_a_person_sees_and_caps_the_size()
    {
        Assert.False(TextLength.Exceeds(new string('é', 5), 5));
        Assert.False(TextLength.Exceeds(string.Concat(Enumerable.Repeat("\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 5)), 5));
        Assert.True(TextLength.Exceeds(new string('a', 6), 5));
        Assert.True(TextLength.Exceeds("e" + new string('\u0301', 1000), 5));
    }

    [Fact]
    public void Shorten_cuts_on_whole_characters()
    {
        string value = string.Concat(Enumerable.Repeat("\U0001F600", 30));

        string shortened = TextLength.Shorten(value);

        Assert.Equal(string.Concat(Enumerable.Repeat("\U0001F600", 20)) + "…", shortened);
        Assert.Equal("short", TextLength.Shorten("short"));
    }
}

using AssetForge.Application;
using AssetForge.Application.Projects;

namespace AssetForge.Tests.Unit;

/// <summary>The FR-1 name rules, without a host or a database.</summary>
public sealed class ProjectNameValidatorTests
{
    private static string[] NameErrors(string? name) =>
        Assert.Throws<ValidationFailedException>(() => ProjectNameValidator.Validate(name)).Errors["name"];

    [Fact]
    public void A_name_is_trimmed_and_put_in_unicode_form_C()
    {
        Assert.Equal("Caf\u00E9", ProjectNameValidator.Validate("  Cafe\u0301 "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void A_blank_name_is_refused(string? name)
    {
        Assert.Equal("Name is required.", Assert.Single(NameErrors(name)));
    }

    [Fact]
    public void The_limit_counts_characters_not_code_units()
    {
        string fifty = string.Concat(Enumerable.Repeat("\U0001F409", 50));   // each is two UTF-16 units

        Assert.Equal(fifty, ProjectNameValidator.Validate(fifty));
        Assert.Equal("Name must be at most 50 characters.", Assert.Single(NameErrors(fifty + "x")));
    }

    [Fact]
    public void A_huge_name_of_stacked_marks_is_refused_as_too_large()
    {
        string huge = "a" + new string('\u0300', 200_000);   // one character a person sees

        Assert.Equal("Name is too large.", Assert.Single(NameErrors(huge)));
    }

    [Theory]
    [InlineData("x\ny")]
    [InlineData("a\u0000b")]
    [InlineData("Dun\u200Bgeon")]
    public void Control_and_invisible_characters_are_refused(string name)
    {
        Assert.Contains("control", Assert.Single(NameErrors(name)));
    }

    [Fact]
    public void Zero_width_joiners_used_by_some_scripts_and_emoji_are_allowed()
    {
        string name = "Family \U0001F468\u200D\U0001F469\u200D\U0001F467";

        Assert.Equal(name, ProjectNameValidator.Validate(name));
    }

    [Fact]
    public void A_new_project_gets_cleaned_names_and_a_version_7_id()
    {
        var project = Project.Create(" cafe\u0301 ", "cafe", DateTimeOffset.UnixEpoch);

        Assert.Equal("caf\u00E9", project.Name);
        Assert.Equal("CAF\u00C9", project.NormalizedName);
        Assert.Equal(7, project.Id.Version);
        Assert.Equal(DateTimeOffset.UnixEpoch, project.CreatedAt);
    }
}

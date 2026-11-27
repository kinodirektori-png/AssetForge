using AssetForge.Application.Projects;

namespace AssetForge.Tests.Unit;

/// <summary>FR-1 slugs, used later in export file names (FR-13): lower-case letters, digits and single hyphens.</summary>
public sealed class SlugsTests
{
    [Theory]
    [InlineData("Dungeon", "dungeon")]
    [InlineData("Mossy Stone  Door", "mossy-stone-door")]
    [InlineData("  --Dark__Forest--  ", "dark-forest")]
    [InlineData("Château Noir", "chateau-noir")]
    [InlineData("Level 2", "level-2")]
    [InlineData("!!!", "fallback")]
    [InlineData("", "fallback")]
    [InlineData("龍", "fallback")]
    public void Builds_a_safe_slug(string text, string expected)
    {
        Assert.Equal(expected, Slugs.From(text, "fallback"));
    }

    [Fact]
    public void Long_names_are_cut_without_a_trailing_hyphen()
    {
        string slug = Slugs.From(new string('a', 29) + " bbbb", "x");

        Assert.Equal(new string('a', 29), slug);
        Assert.True(Slugs.From(new string('a', 60), "x").Length <= Slugs.MaxLength);
    }

    [Fact]
    public void A_taken_slug_gets_the_next_free_number()
    {
        Assert.Equal("dungeon", Slugs.MakeUnique("dungeon", new HashSet<string>()));
        Assert.Equal("dungeon-2", Slugs.MakeUnique("dungeon", new HashSet<string> { "dungeon" }));
        Assert.Equal("dungeon-4", Slugs.MakeUnique("dungeon", new HashSet<string> { "dungeon", "dungeon-2", "dungeon-3" }));
    }

    [Fact]
    public void A_numbered_slug_stays_within_the_maximum_length()
    {
        string full = new string('a', Slugs.MaxLength);

        string? unique = Slugs.MakeUnique(full, new HashSet<string> { full });

        Assert.Equal(new string('a', Slugs.MaxLength - 2) + "-2", unique);
    }

    [Fact]
    public void Shared_prefix_covers_every_numbered_variant()
    {
        string full = new('a', Slugs.MaxLength);
        string prefix = Slugs.SharedPrefix(full);

        Assert.StartsWith(prefix, Slugs.MakeUnique(full, new HashSet<string> { full }));
        Assert.StartsWith(prefix, Slugs.MakeUnique(full, new HashSet<string> { full, full[..^2] + "-2" })!);
        Assert.Equal(new string('b', 24), Slugs.SharedPrefix(new string('b', 24) + "-" + new string('c', 10)));
        Assert.Equal("short", Slugs.SharedPrefix("short"));
    }

    [Fact]
    public void Giving_up_after_9999_variants_returns_null_not_an_endless_loop()
    {
        var taken = Enumerable.Range(2, 9998).Select(n => $"dungeon-{n}").Append("dungeon").ToHashSet();

        Assert.Null(Slugs.MakeUnique("dungeon", taken));
        Assert.Equal("dungeon-9999", Slugs.MakeUnique("dungeon", taken.Where(t => t != "dungeon-9999").ToHashSet()));
    }
}

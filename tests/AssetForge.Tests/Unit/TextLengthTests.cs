using AssetForge.Application;

namespace AssetForge.Tests.Unit;

public sealed class TextLengthTests
{
    [Fact]
    public void Characters_are_counted_as_a_person_sees_them()
    {
        Assert.Equal(TextLimit.Within, TextLength.Check(new string('\u00E9', 5), 5));
        Assert.Equal(TextLimit.Within, TextLength.Check(string.Concat(Enumerable.Repeat("\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466", 5)), 5));
        Assert.Equal(TextLimit.TooManyCharacters, TextLength.Check(new string('a', 6), 5));
    }

    [Fact]
    public void Stacked_marks_are_one_character_but_are_still_capped_in_size()
    {
        Assert.Equal(TextLimit.TooLarge, TextLength.Check("e" + new string('\u0301', 1000), 5));
    }

    [Fact]
    public void Shorten_cuts_on_whole_characters()
    {
        string value = string.Concat(Enumerable.Repeat("\U0001F600", 30));

        Assert.Equal(string.Concat(Enumerable.Repeat("\U0001F600", 20)) + "…", TextLength.Shorten(value));
        Assert.Equal("short", TextLength.Shorten("short"));
    }
}

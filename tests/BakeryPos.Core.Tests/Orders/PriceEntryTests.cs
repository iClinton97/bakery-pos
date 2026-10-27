using BakeryPos.Core.Orders;
using Xunit;

namespace BakeryPos.Core.Tests.Orders;

public class PriceEntryTests
{
    private static PriceEntry Type(string keys)
    {
        var entry = new PriceEntry();
        foreach (var key in keys)
        {
            entry.Press(key);
        }

        return entry;
    }

    [Fact]
    public void NewEntry_IsEmpty_AndZero()
    {
        var entry = new PriceEntry();

        Assert.True(entry.IsEmpty);
        Assert.Equal(0, entry.Cents);
        Assert.Equal("$0.00", entry.DisplayText);
    }

    [Theory]
    [InlineData("3", 300)]
    [InlineData("3.5", 350)]
    [InlineData("3.50", 350)]
    [InlineData("12.05", 1205)]
    [InlineData(".5", 50)]
    [InlineData("0.99", 99)]
    public void Cents_MatchesWhatWasTyped(string keys, int expected)
    {
        Assert.Equal(expected, Type(keys).Cents);
    }

    [Fact]
    public void ThirdDecimalDigit_IsIgnored()
    {
        Assert.Equal(350, Type("3.509").Cents);
    }

    [Fact]
    public void SecondDot_IsIgnored()
    {
        Assert.Equal(350, Type("3..50").Cents);
    }

    [Fact]
    public void LeadingZero_IsReplaced()
    {
        Assert.Equal(500, Type("05").Cents);
    }

    [Fact]
    public void WholeDigits_AreLimitedToFour()
    {
        Assert.Equal(999900, Type("99999").Cents);
    }

    [Fact]
    public void Backspace_RemovesTheLastKey()
    {
        var entry = Type("3.5");

        entry.Backspace();

        Assert.Equal(300, entry.Cents);
    }

    [Fact]
    public void Backspace_OnEmptyEntry_DoesNothing()
    {
        var entry = new PriceEntry();

        entry.Backspace();

        Assert.True(entry.IsEmpty);
    }

    [Fact]
    public void Clear_EmptiesTheEntry()
    {
        var entry = Type("12.5");

        entry.Clear();

        Assert.True(entry.IsEmpty);
    }

    [Fact]
    public void OtherKeys_AreIgnored()
    {
        Assert.Equal(300, Type("3x-").Cents);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2000, 2000)]
    [InlineData(1550, 1550)]
    [InlineData(5, 5)]
    public void SetCents_ReplacesWhatWasTyped(int cents, int expected)
    {
        var entry = Type("99");

        entry.SetCents(cents);

        Assert.Equal(expected, entry.Cents);
    }

    [Fact]
    public void SetCents_RejectsNegativeAmounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PriceEntry().SetCents(-1));
    }

    [Fact]
    public void SetCents_ThenTyping_Continues()
    {
        var entry = new PriceEntry();
        entry.SetCents(2000);

        entry.Press('5');

        Assert.Equal(20500, entry.Cents);
    }
}

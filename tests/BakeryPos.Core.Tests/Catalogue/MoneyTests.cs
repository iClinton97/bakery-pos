using BakeryPos.Core.Catalogue;
using Xunit;

namespace BakeryPos.Core.Tests.Catalogue;

public class MoneyTests
{
    [Theory]
    [InlineData(0, "$0.00")]
    [InlineData(5, "$0.05")]
    [InlineData(550, "$5.50")]
    [InlineData(128460, "$1,284.60")]
    [InlineData(-100, "-$1.00")]
    public void Format_ShowsDollarsAndCents(int cents, string expected)
    {
        Assert.Equal(expected, Money.Format(cents));
    }
}

using System;
using System.Linq;
using BakeryPos.Core.Catalogue;
using Xunit;

namespace BakeryPos.Core.Tests.Catalogue;

public class ProductCatalogueTests
{
    private static readonly ProductCatalogue Sample = SampleCatalogue.Create();

    [Theory]
    [InlineData(Category.Pies, 26)]
    [InlineData(Category.HotFood, 8)]
    [InlineData(Category.Cakes, 11)]
    [InlineData(Category.Savoury, 10)]
    public void Sample_HasExpectedProductCount(Category category, int expected)
    {
        Assert.Equal(expected, Sample.InCategory(category).Count);
    }

    [Fact]
    public void Popular_PiesAreInRankOrder()
    {
        var names = Sample.Popular(Category.Pies).Select(p => p.Name).ToArray();

        Assert.Equal(
            new[] { "Plain Pies", "Sausage Rolls", "Pepper", "Mushroom", "Hot Chilli", "Meat Pastie", "Party Pie", "Party Sausage Rolls" },
            names);
    }

    [Fact]
    public void Popular_AndNotPopular_TogetherCoverEveryPie()
    {
        var all = Sample.InCategory(Category.Pies).Count;

        Assert.Equal(all, Sample.Popular(Category.Pies).Count + Sample.NotPopular(Category.Pies).Count);
    }

    [Fact]
    public void Every_Pie_HasAPieType_AndNothingElseDoes()
    {
        Assert.All(Sample.InCategory(Category.Pies), p => Assert.NotNull(p.PieType));
        Assert.All(Sample.All.Where(p => p.Category != Category.Pies), p => Assert.Null(p.PieType));
    }

    [Fact]
    public void Ids_AreSluggedFromNames()
    {
        Assert.Equal("bacon-and-egg", new Product("Bacon & Egg", Category.Pies, 650, 12, 5).Id);
        Assert.Equal("large-donut-no-jam", new Product("Large Donut (no jam)", Category.Savoury, 300, 24, 6).Id);
    }

    [Fact]
    public void Find_ReturnsTheProduct_OrNull()
    {
        Assert.Equal("Skewers", Sample.Find("skewers")?.Name);
        Assert.Null(Sample.Find("not-a-product"));
    }

    [Fact]
    public void Catalogue_RejectsDuplicateIds()
    {
        var products = new[]
        {
            new Product("Hot Dog", Category.HotFood, 450, 24, 6),
            new Product("hot dog", Category.HotFood, 450, 24, 6),
        };

        Assert.Throws<ArgumentException>(() => new ProductCatalogue(products));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Catalogue_RejectsNonPositivePrices(int cents)
    {
        var products = new[] { new Product("Free Pie", Category.Pies, cents, 12, 5) };

        Assert.Throws<ArgumentException>(() => new ProductCatalogue(products));
    }

    [Fact]
    public void Catalogue_RejectsAlertAbovePar()
    {
        var products = new[] { new Product("Odd Pie", Category.Pies, 600, 5, 8) };

        Assert.Throws<ArgumentException>(() => new ProductCatalogue(products));
    }
}

using System;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;
using Xunit;

namespace BakeryPos.Core.Tests.Orders;

public class OrderTests
{
    private static readonly Product Pie = new("Plain Pies", Category.Pies, 550, 36, 8);
    private static readonly Product Dimmy = new("Large dim sim", Category.HotFood, 250, 40, 10);

    [Fact]
    public void NewOrder_IsEmpty_WithZeroTotal()
    {
        var order = new Order();

        Assert.True(order.IsEmpty);
        Assert.Equal(0, order.TotalCents);
    }

    [Fact]
    public void Add_SameProductTwice_IncreasesQuantity()
    {
        var order = new Order();

        order.Add(Pie);
        order.Add(Pie);

        var line = Assert.Single(order.Lines);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(1100, line.LineTotalCents);
    }

    [Fact]
    public void Total_SumsAllLines()
    {
        var order = new Order();

        order.Add(Pie);
        order.Add(Pie);
        order.Add(Dimmy);

        Assert.Equal(1350, order.TotalCents);
    }

    [Fact]
    public void Decrease_RemovesTheLine_WhenQuantityReachesZero()
    {
        var order = new Order();
        order.Add(Pie);

        order.Decrease(Pie.Id);

        Assert.True(order.IsEmpty);
    }

    [Fact]
    public void Decrease_KeepsTheLine_WhenQuantityIsAboveOne()
    {
        var order = new Order();
        order.Add(Pie);
        order.Increase(Pie.Id);

        order.Decrease(Pie.Id);

        Assert.Equal(1, Assert.Single(order.Lines).Quantity);
    }

    [Fact]
    public void Remove_DeletesTheWholeLine()
    {
        var order = new Order();
        order.Add(Pie);
        order.Increase(Pie.Id);
        order.Add(Dimmy);

        order.Remove(Pie.Id);

        Assert.Equal("Large dim sim", Assert.Single(order.Lines).Name);
    }

    [Fact]
    public void Miscellaneous_AddsSeparateLines_WithNoProduct()
    {
        var order = new Order();

        order.AddMiscellaneous(350);
        order.AddMiscellaneous(350);

        Assert.Equal(2, order.Lines.Count);
        Assert.All(order.Lines, l => Assert.True(l.IsMiscellaneous));
        Assert.Equal(700, order.TotalCents);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Miscellaneous_RejectsNonPositivePrices(int cents)
    {
        var order = new Order();

        Assert.Throws<ArgumentOutOfRangeException>(() => order.AddMiscellaneous(cents));
    }

    [Fact]
    public void UnknownLine_Throws()
    {
        var order = new Order();

        Assert.Throws<ArgumentException>(() => order.Increase("nope"));
    }

    [Fact]
    public void Clear_EmptiesTheOrder()
    {
        var order = new Order();
        order.Add(Pie);

        order.Clear();

        Assert.True(order.IsEmpty);
    }
}

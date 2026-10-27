using System;
using System.Linq;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Stock;
using Xunit;

namespace BakeryPos.Core.Tests.Stock;

public class StockReportTests
{
    private static readonly Product Pie = new("Plain Pies", Category.Pies, 550, 20, 5);
    private static readonly Product Dimmy = new("Large dim sim", Category.HotFood, 250, 40, 10);
    private static readonly Product Tart = new("Fruit Tart", Category.Cakes, 450, 12, 3);

    private static readonly DateTimeOffset Now = new(2026, 10, 27, 10, 42, 0, TimeSpan.Zero);

    private static (ProductCatalogue Catalogue, StockBoard Board) Setup()
    {
        var catalogue = new ProductCatalogue([Pie, Dimmy, Tart]);
        return (catalogue, new StockBoard(catalogue));
    }

    [Fact]
    public void Rows_HaveEveryProduct_WithTheirCounts()
    {
        var (catalogue, board) = Setup();
        board.SetBooth(Pie.Id, 7);
        board.SetFridge(Pie.Id, 30);

        var report = StockReport.Build(catalogue, board, Now);

        var row = Assert.Single(report.Rows, r => r.Name == "Plain Pies");
        Assert.Equal(3, report.Rows.Count);
        Assert.Equal(7, row.Booth);
        Assert.Equal(30, row.Fridge);
        Assert.Equal(20, row.Par);
        Assert.Equal(Now, report.GeneratedAt);
    }

    [Fact]
    public void Rows_AreOrdered_SoldOutThenLowThenOk()
    {
        var (catalogue, board) = Setup();
        board.SetBooth(Pie.Id, 4);
        board.SetBooth(Dimmy.Id, 0);

        var names = StockReport.Build(catalogue, board, Now).Rows.Select(r => r.Name).ToArray();

        Assert.Equal(new[] { "Large dim sim", "Plain Pies", "Fruit Tart" }, names);
    }

    [Fact]
    public void Refills_MatchTheStockBoard()
    {
        var (catalogue, board) = Setup();
        board.SetBooth(Pie.Id, 3);
        board.SetFridge(Pie.Id, 10);

        var refill = Assert.Single(StockReport.Build(catalogue, board, Now).Refills);

        Assert.Equal("Plain Pies", refill.Name);
        Assert.Equal(10, refill.BringOut);
        Assert.Equal(7, refill.FridgeShortfall);
    }

    [Fact]
    public void ALiveFeed_ServesTheLatestReport()
    {
        var (catalogue, board) = Setup();
        var feed = new LiveStockFeed();
        Assert.Null(feed.Current);

        feed.Publish(StockReport.Build(catalogue, board, Now));
        var later = StockReport.Build(catalogue, board, Now.AddMinutes(1));
        feed.Publish(later);

        Assert.Same(later, feed.Current);
    }
}

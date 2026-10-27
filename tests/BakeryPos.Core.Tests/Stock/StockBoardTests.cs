using System;
using System.Collections.Generic;
using System.Linq;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;
using BakeryPos.Core.Payments;
using BakeryPos.Core.Sales;
using BakeryPos.Core.Stock;
using Xunit;

namespace BakeryPos.Core.Tests.Stock;

public class StockBoardTests
{
    // Par 20, low at 5 or fewer.
    private static readonly Product Pie = new("Plain Pies", Category.Pies, 550, 20, 5);
    private static readonly Product Dimmy = new("Large dim sim", Category.HotFood, 250, 40, 10);

    private static StockBoard NewBoard() => new(new ProductCatalogue([Pie, Dimmy]));

    private static CompletedSale Sell(params (Product Product, int Quantity)[] items)
    {
        var order = new Order();
        foreach (var (product, quantity) in items)
        {
            for (var i = 0; i < quantity; i++)
            {
                order.Add(product);
            }
        }

        return new Checkout(new MockPaymentTerminal()).PayWithCash(order, "0001", order.TotalCents);
    }

    [Fact]
    public void NewBoard_StartsWithAFullBooth_AndAnEmptyFridge()
    {
        var board = NewBoard();

        Assert.Equal(20, board.BoothCount(Pie.Id));
        Assert.Equal(0, board.FridgeCount(Pie.Id));
        Assert.Equal(StockStatus.Ok, board.StatusOf(Pie.Id));
    }

    [Theory]
    [InlineData(6, StockStatus.Ok)]
    [InlineData(5, StockStatus.Low)]
    [InlineData(1, StockStatus.Low)]
    [InlineData(0, StockStatus.SoldOut)]
    public void Status_FollowsTheAlertLevel(int booth, StockStatus expected)
    {
        var board = NewBoard();

        board.SetBooth(Pie.Id, booth);

        Assert.Equal(expected, board.StatusOf(Pie.Id));
    }

    [Fact]
    public void Deduct_TakesSoldItemsOutOfTheBooth()
    {
        var board = NewBoard();

        var changes = board.Deduct(Sell((Pie, 3), (Dimmy, 2)));

        Assert.Equal(17, board.BoothCount(Pie.Id));
        Assert.Equal(38, board.BoothCount(Dimmy.Id));
        Assert.Equal(2, changes.Count);
    }

    [Fact]
    public void Deduct_ReportsWhatWasSoldAndWhatIsLeft()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 7);

        var change = Assert.Single(board.Deduct(Sell((Pie, 2))));

        Assert.Equal(2, change.Sold);
        Assert.Equal(5, change.BoothAfter);
        Assert.Equal(StockStatus.Low, change.Status);
    }

    [Fact]
    public void Deduct_NeverGoesBelowZero()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 1);

        board.Deduct(Sell((Pie, 3)));

        Assert.Equal(0, board.BoothCount(Pie.Id));
        Assert.Equal(StockStatus.SoldOut, board.StatusOf(Pie.Id));
    }

    [Fact]
    public void Deduct_IgnoresMiscellaneousLines()
    {
        var board = NewBoard();
        var order = new Order();
        order.AddMiscellaneous(350);
        var sale = new Checkout(new MockPaymentTerminal()).PayWithCash(order, "0002", 350);

        var changes = board.Deduct(sale);

        Assert.Empty(changes);
        Assert.Equal(20, board.BoothCount(Pie.Id));
    }

    [Fact]
    public void MoveToBooth_MovesStockFromTheFridge()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 2);
        board.SetFridge(Pie.Id, 30);

        var moved = board.MoveToBooth(Pie.Id, 18);

        Assert.Equal(18, moved);
        Assert.Equal(20, board.BoothCount(Pie.Id));
        Assert.Equal(12, board.FridgeCount(Pie.Id));
    }

    [Fact]
    public void MoveToBooth_IsLimitedToWhatTheFridgeHolds()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 2);
        board.SetFridge(Pie.Id, 6);

        var moved = board.MoveToBooth(Pie.Id, 18);

        Assert.Equal(6, moved);
        Assert.Equal(8, board.BoothCount(Pie.Id));
        Assert.Equal(0, board.FridgeCount(Pie.Id));
    }

    [Fact]
    public void RefillList_IsEmpty_WhenEverythingIsOk()
    {
        Assert.Empty(NewBoard().RefillList());
    }

    [Fact]
    public void RefillList_ShowsLowAndSoldOut_MostUrgentFirst()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 3);
        board.SetBooth(Dimmy.Id, 0);

        var list = board.RefillList();

        Assert.Equal([Dimmy.Name, Pie.Name], list.Select(i => i.Product.Name).ToArray());
        Assert.Equal(StockStatus.SoldOut, list[0].Status);
    }

    [Fact]
    public void RefillList_BringOutIsLimitedByTheFridge_AndReportsTheShortfall()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 3);
        board.SetFridge(Pie.Id, 10);

        var item = Assert.Single(board.RefillList());

        Assert.Equal(10, item.BringOut);
        Assert.Equal(7, item.FridgeShortfall);
    }

    [Fact]
    public void RefillList_BringOutIsTheGapToPar_WhenTheFridgeHasEnough()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 3);
        board.SetFridge(Pie.Id, 50);

        var item = Assert.Single(board.RefillList());

        Assert.Equal(17, item.BringOut);
        Assert.Equal(0, item.FridgeShortfall);
    }

    [Fact]
    public void Snapshot_ThenRestore_BringsCountsBack()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 7);
        board.SetFridge(Pie.Id, 33);

        var fresh = NewBoard();
        var restored = fresh.Restore(board.Snapshot());

        Assert.Equal(2, restored);
        Assert.Equal(7, fresh.BoothCount(Pie.Id));
        Assert.Equal(33, fresh.FridgeCount(Pie.Id));
    }

    [Fact]
    public void Restore_SkipsProductsThatAreNoLongerInTheCatalogue()
    {
        var board = NewBoard();

        var restored = board.Restore([new StockSnapshot("gone", 5, 5), new StockSnapshot(Pie.Id, 9, 1)]);

        Assert.Equal(1, restored);
        Assert.Equal(9, board.BoothCount(Pie.Id));
    }

    [Fact]
    public void UnknownProduct_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewBoard().BoothCount("nope"));
    }

    [Fact]
    public void NegativeCounts_AreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewBoard().SetBooth(Pie.Id, -1));
    }
}

public class SampleStockTests
{
    [Fact]
    public void SampleStock_NamesOnlyRealProducts_AndShowsWarnings()
    {
        var catalogue = SampleCatalogue.Create();

        var board = SampleStock.Create(catalogue);

        Assert.Equal(StockStatus.SoldOut, board.StatusOf("tenders"));
        Assert.Equal(StockStatus.Low, board.StatusOf("hot-chilli"));
        Assert.Equal(StockStatus.Ok, board.StatusOf("plain-pies"));
        Assert.Equal(48, board.FridgeCount("plain-pies"));
    }
}

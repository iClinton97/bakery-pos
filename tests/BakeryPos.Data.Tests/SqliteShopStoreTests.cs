using System;
using System.IO;
using System.Threading.Tasks;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;
using BakeryPos.Core.Payments;
using BakeryPos.Core.Sales;
using BakeryPos.Core.Stock;
using BakeryPos.Data;
using Xunit;

namespace BakeryPos.Data.Tests;

public class SqliteShopStoreTests : IDisposable
{
    private static readonly Product Pie = new("Plain Pies", Category.Pies, 550, 20, 5);
    private static readonly Product Dimmy = new("Large dim sim", Category.HotFood, 250, 40, 10);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bakerypos-tests-" + Guid.NewGuid().ToString("N"));
    private readonly SqliteShopStore _store;

    public SqliteShopStoreTests()
    {
        _store = new SqliteShopStore(Path.Combine(_folder, "shop.db"));
        _store.Initialise();
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static StockBoard NewBoard() => new(new ProductCatalogue([Pie, Dimmy]));

    private static CompletedSale CashSale(string reference)
    {
        var order = new Order();
        order.Add(Pie);
        order.Add(Pie);
        order.Add(Dimmy);
        order.AddMiscellaneous(350);
        return new Checkout(new MockPaymentTerminal()).PayWithCash(order, reference, 3000);
    }

    [Fact]
    public void Initialise_CanRunTwice()
    {
        _store.Initialise();

        Assert.Empty(_store.LoadSales());
    }

    [Fact]
    public void RecordSale_ThenLoadSales_RoundTripsASale()
    {
        var sale = CashSale("0001");

        _store.RecordSale(sale, NewBoard());
        var loaded = Assert.Single(_store.LoadSales());

        Assert.Equal("0001", loaded.Reference);
        Assert.Equal(sale.TotalCents, loaded.TotalCents);
        Assert.Equal(PaymentMethod.Cash, loaded.Method);
        Assert.Equal(3000, loaded.CashReceivedCents);
        Assert.Equal(sale.CompletedAt.UtcDateTime, loaded.CompletedAt.UtcDateTime);
        Assert.Equal(sale.Lines, loaded.Lines);
        Assert.Equal(sale.Payments, loaded.Payments);
    }

    [Fact]
    public void Miscellaneous_Lines_AreSavedWithoutAProductId()
    {
        _store.RecordSale(CashSale("0001"), NewBoard());

        var loaded = Assert.Single(_store.LoadSales());

        Assert.Contains(loaded.Lines, l => l.IsMiscellaneous && l.UnitPriceCents == 350);
    }

    [Fact]
    public async Task SplitSale_KeepsBothPayments()
    {
        var order = new Order();
        order.Add(Pie);
        order.Add(Dimmy);
        var result = await new Checkout(new MockPaymentTerminal()).PayBySplitAsync(order, "0002", 500);
        var sale = Assert.IsType<CompletedSale>(result.Sale);

        _store.RecordSale(sale, NewBoard());
        var loaded = Assert.Single(_store.LoadSales());

        Assert.Equal(PaymentMethod.Split, loaded.Method);
        Assert.Equal(500, loaded.CashCents);
        Assert.Equal(300, loaded.CardCents);
    }

    [Fact]
    public void RecordSale_AlsoSavesStock_ThatLoadStockBringsBack()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 7);
        board.SetFridge(Pie.Id, 33);

        _store.RecordSale(CashSale("0001"), board);
        var fresh = NewBoard();
        var restored = _store.LoadStock(fresh);

        Assert.Equal(2, restored);
        Assert.Equal(7, fresh.BoothCount(Pie.Id));
        Assert.Equal(33, fresh.FridgeCount(Pie.Id));
    }

    [Fact]
    public void SaveStock_Twice_OverwritesTheOldCounts()
    {
        var board = NewBoard();
        board.SetBooth(Pie.Id, 9);
        _store.SaveStock(board);
        board.SetBooth(Pie.Id, 4);
        _store.SaveStock(board);

        var fresh = NewBoard();
        _store.LoadStock(fresh);

        Assert.Equal(4, fresh.BoothCount(Pie.Id));
    }

    [Fact]
    public void LoadStock_OnAnEmptyDatabase_RestoresNothing()
    {
        Assert.Equal(0, _store.LoadStock(NewBoard()));
    }

    [Fact]
    public void NextOrderNumber_StartsAtOne_AndFollowsTheHighestSaved()
    {
        Assert.Equal(1, _store.NextOrderNumber());

        _store.RecordSale(CashSale("0007"), NewBoard());

        Assert.Equal(8, _store.NextOrderNumber());
    }

    [Fact]
    public void Data_SurvivesOpeningTheFileAgain()
    {
        _store.RecordSale(CashSale("0001"), NewBoard());

        var reopened = new SqliteShopStore(_store.DatabasePath);
        reopened.Initialise();

        Assert.Single(reopened.LoadSales());
    }
}

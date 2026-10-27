using System;
using System.Collections.Generic;
using System.Linq;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Sales;

namespace BakeryPos.Core.Stock;

public enum StockStatus
{
    Ok,

    /// <summary>At or below the product's alert level.</summary>
    Low,

    SoldOut,
}

/// <summary>What one product looks like after a sale: how many were taken and how many are left in the booth.</summary>
public sealed record StockChange(Product Product, int Sold, int BoothAfter, StockStatus Status);

/// <summary>A product that needs refilling, and how much to bring out of the fridge.</summary>
/// <param name="BringOut">What can be moved now: the gap to par, limited to what is in the fridge.</param>
/// <param name="FridgeShortfall">How much is still missing because the fridge does not hold enough.</param>
public sealed record RefillItem(Product Product, int Booth, int Fridge, int BringOut, int FridgeShortfall, StockStatus Status);

/// <summary>
/// Live stock counts. The booth is what is out and ready to sell, and the cashier sells from it.
/// The fridge is back-of-house storage that refills the booth.
/// </summary>
public sealed class StockBoard
{
    private sealed class Level
    {
        public int Booth;
        public int Fridge;
    }

    private readonly ProductCatalogue _catalogue;
    private readonly Dictionary<string, Level> _levels = new();

    /// <summary>Starts every product with a full booth (its par level) and an empty fridge.</summary>
    public StockBoard(ProductCatalogue catalogue)
    {
        _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));

        foreach (var product in catalogue.All)
        {
            _levels[product.Id] = new Level { Booth = product.ParLevel };
        }
    }

    public int BoothCount(string productId) => Get(productId).Booth;

    public int FridgeCount(string productId) => Get(productId).Fridge;

    public StockStatus StatusOf(string productId)
    {
        var product = FindProduct(productId);
        var booth = Get(productId).Booth;

        if (booth <= 0)
        {
            return StockStatus.SoldOut;
        }

        return booth <= product.AlertAt ? StockStatus.Low : StockStatus.Ok;
    }

    public void SetBooth(string productId, int count) => Get(productId).Booth = RequireNotNegative(count);

    public void SetFridge(string productId, int count) => Get(productId).Fridge = RequireNotNegative(count);

    /// <summary>
    /// Takes the sold items out of the booth. Miscellaneous lines are ignored because they have no stock.
    /// The booth never goes below zero, even if more was sold than the count said was there.
    /// </summary>
    public IReadOnlyList<StockChange> Deduct(CompletedSale sale)
    {
        ArgumentNullException.ThrowIfNull(sale);

        var changes = new List<StockChange>();

        foreach (var line in sale.Lines.Where(l => l.ProductId is not null))
        {
            var level = Get(line.ProductId!);
            level.Booth = Math.Max(0, level.Booth - line.Quantity);
            changes.Add(new StockChange(
                FindProduct(line.ProductId!),
                line.Quantity,
                level.Booth,
                StatusOf(line.ProductId!)));
        }

        return changes;
    }

    /// <summary>Moves stock from the fridge to the booth. Returns how many were actually moved.</summary>
    public int MoveToBooth(string productId, int quantity)
    {
        RequireNotNegative(quantity);

        var level = Get(productId);
        var moved = Math.Min(quantity, level.Fridge);
        level.Fridge -= moved;
        level.Booth += moved;
        return moved;
    }

    /// <summary>Everything that is low or sold out, most urgent first.</summary>
    public IReadOnlyList<RefillItem> RefillList()
    {
        var items = new List<RefillItem>();

        foreach (var product in _catalogue.All)
        {
            var status = StatusOf(product.Id);
            if (status == StockStatus.Ok)
            {
                continue;
            }

            var level = _levels[product.Id];
            var wanted = Math.Max(0, product.ParLevel - level.Booth);
            var bringOut = Math.Min(wanted, level.Fridge);
            items.Add(new RefillItem(product, level.Booth, level.Fridge, bringOut, wanted - bringOut, status));
        }

        return items
            .OrderBy(i => i.Status == StockStatus.SoldOut ? 0 : 1)
            .ThenBy(i => i.Booth)
            .ToList();
    }

    private Level Get(string productId) =>
        _levels.TryGetValue(productId, out var level)
            ? level
            : throw new ArgumentException($"No product '{productId}' in the catalogue.", nameof(productId));

    private Product FindProduct(string productId) =>
        _catalogue.Find(productId) ?? throw new ArgumentException($"No product '{productId}' in the catalogue.", nameof(productId));

    private static int RequireNotNegative(int count) =>
        count >= 0 ? count : throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
}

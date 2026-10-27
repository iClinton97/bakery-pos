using System;
using System.Collections.Generic;
using System.Linq;
using BakeryPos.Core.Catalogue;

namespace BakeryPos.Core.Stock;

/// <summary>One product's counts, as shown on the live stock page.</summary>
public sealed record StockRow(string Name, Category Category, int Booth, int Fridge, int Par, StockStatus Status);

/// <summary>One entry on the refill list.</summary>
public sealed record RefillRow(
    string Name,
    Category Category,
    int Booth,
    int Fridge,
    int BringOut,
    int FridgeShortfall,
    StockStatus Status);

/// <summary>
/// A frozen picture of all stock at one moment. It never changes after it is built, so it is safe
/// to hand to the web server's threads while the till keeps selling.
/// </summary>
public sealed record StockReport(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<StockRow> Rows,
    IReadOnlyList<RefillRow> Refills)
{
    /// <summary>Rows come out most urgent first: sold out, then low, then OK, each from fewest to most in the booth.</summary>
    public static StockReport Build(ProductCatalogue catalogue, StockBoard board, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(board);

        var rows = catalogue.All
            .Select(p => new StockRow(
                p.Name,
                p.Category,
                board.BoothCount(p.Id),
                board.FridgeCount(p.Id),
                p.ParLevel,
                board.StatusOf(p.Id)))
            .OrderBy(r => r.Status switch { StockStatus.SoldOut => 0, StockStatus.Low => 1, _ => 2 })
            .ThenBy(r => r.Booth)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var refills = board.RefillList()
            .Select(r => new RefillRow(
                r.Product.Name,
                r.Product.Category,
                r.Booth,
                r.Fridge,
                r.BringOut,
                r.FridgeShortfall,
                r.Status))
            .ToList();

        return new StockReport(now, rows, refills);
    }
}

/// <summary>
/// Hands the latest stock picture from the till to the web page. The till publishes a new report after
/// each sale, and the web server only ever reads the newest one.
/// </summary>
public sealed class LiveStockFeed
{
    private volatile StockReport? _current;

    public StockReport? Current => _current;

    public void Publish(StockReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _current = report;
    }
}

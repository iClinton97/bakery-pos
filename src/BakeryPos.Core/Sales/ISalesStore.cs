using BakeryPos.Core.Stock;

namespace BakeryPos.Core.Sales;

/// <summary>
/// Where finished sales and stock are kept. The app talks to this interface, so the
/// storage (a SQLite file today) can change without touching the till screens.
/// </summary>
public interface ISalesStore
{
    /// <summary>Saves the sale and the stock counts together, so the two never disagree.</summary>
    void RecordSale(CompletedSale sale, StockBoard stock);

    /// <summary>The number to give the next order, one more than the highest saved so far.</summary>
    int NextOrderNumber();
}

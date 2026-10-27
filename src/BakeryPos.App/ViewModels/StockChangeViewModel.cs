using Avalonia.Media;
using BakeryPos.Core.Stock;

namespace BakeryPos.App.ViewModels;

/// <summary>One line in the "Stock updated" list shown after a sale.</summary>
public sealed class StockChangeViewModel
{
    public StockChangeViewModel(StockChange change)
    {
        Name = change.Product.Name;

        var suffix = change.Status switch
        {
            StockStatus.SoldOut => " · Sold out",
            StockStatus.Low => " · Low",
            _ => "",
        };

        Detail = $"-{change.Sold} · {change.BoothAfter} left{suffix}";
        Foreground = new SolidColorBrush(Color.Parse(change.Status switch
        {
            StockStatus.SoldOut => "#A32020",
            StockStatus.Low => "#B4440B",
            _ => "#4A5563",
        }));
    }

    public string Name { get; }

    public string Detail { get; }

    public IBrush Foreground { get; }
}

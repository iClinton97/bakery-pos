using System;
using System.Windows.Input;
using Avalonia.Media;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Stock;

namespace BakeryPos.App.ViewModels;

/// <summary>One product button on the cashier screen, with its stock warning.</summary>
public sealed class ProductTileViewModel
{
    private static readonly IBrush Grey = Brush("#D5DAE0");
    private static readonly IBrush Muted = Brush("#4A5563");
    private static readonly IBrush Orange = Brush("#B4440B");
    private static readonly IBrush OrangeTint = Brush("#FBEDE4");
    private static readonly IBrush Red = Brush("#A32020");
    private static readonly IBrush RedTint = Brush("#FBE9E9");

    public ProductTileViewModel(Product product, StockBoard stock, Action<Product> add)
    {
        Name = product.Name;
        PriceText = Money.Format(product.PriceCents);
        TypeLabel = product.PieType is { } type ? PieTypeStyle.Label(type) : null;
        StripeBrush = Brush(product.PieType is { } t ? PieTypeStyle.Colour(t) : "#FFFFFF");
        AddCommand = new RelayCommand(() => add(product));

        var booth = stock.BoothCount(product.Id);
        switch (stock.StatusOf(product.Id))
        {
            case StockStatus.SoldOut:
                LeftText = "Sold out";
                BorderBrush = Red;
                PillBackground = RedTint;
                PillForeground = Red;
                break;
            case StockStatus.Low:
                LeftText = $"Low · {booth} left";
                BorderBrush = Orange;
                PillBackground = OrangeTint;
                PillForeground = Orange;
                break;
            default:
                LeftText = $"{booth} left";
                BorderBrush = Grey;
                PillBackground = Brushes.Transparent;
                PillForeground = Muted;
                break;
        }
    }

    public string Name { get; }

    public string PriceText { get; }

    /// <summary>The pie type shown under the name, or null for products that are not pies.</summary>
    public string? TypeLabel { get; }

    public bool HasType => TypeLabel is not null;

    public IBrush StripeBrush { get; }

    public string LeftText { get; }

    public IBrush BorderBrush { get; }

    public IBrush PillBackground { get; }

    public IBrush PillForeground { get; }

    public ICommand AddCommand { get; }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}

/// <summary>Colours and labels for the pie types, matching the design mockups.</summary>
internal static class PieTypeStyle
{
    public static string Label(PieType type) => type switch
    {
        PieType.Beef => "Beef",
        PieType.Lamb => "Lamb",
        PieType.Chicken => "Chicken",
        PieType.Bacon => "Bacon",
        PieType.SausageRoll => "Sausage roll",
        PieType.VegAndCheese => "Veg and cheese",
        _ => type.ToString(),
    };

    public static string Colour(PieType type) => type switch
    {
        PieType.Beef => "#7A4B2A",
        PieType.Lamb => "#6B3FA0",
        PieType.Chicken => "#A67C00",
        PieType.Bacon => "#B03A6B",
        PieType.SausageRoll => "#0E7C86",
        PieType.VegAndCheese => "#2E7D32",
        _ => "#4A5563",
    };
}

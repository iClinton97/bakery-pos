using System;
using System.Windows.Input;
using Avalonia.Media;
using BakeryPos.Core.Catalogue;

namespace BakeryPos.App.ViewModels;

/// <summary>One product button on the cashier screen.</summary>
public sealed class ProductTileViewModel
{
    public ProductTileViewModel(Product product, Action<Product> add)
    {
        Name = product.Name;
        PriceText = Money.Format(product.PriceCents);
        TypeLabel = product.PieType is { } type ? PieTypeStyle.Label(type) : null;
        StripeBrush = new SolidColorBrush(Color.Parse(
            product.PieType is { } t ? PieTypeStyle.Colour(t) : "#FFFFFF"));
        AddCommand = new RelayCommand(() => add(product));
    }

    public string Name { get; }

    public string PriceText { get; }

    /// <summary>The pie type shown under the name, or null for products that are not pies.</summary>
    public string? TypeLabel { get; }

    public bool HasType => TypeLabel is not null;

    public IBrush StripeBrush { get; }

    public ICommand AddCommand { get; }
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

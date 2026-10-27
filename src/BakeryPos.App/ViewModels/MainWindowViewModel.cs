using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;

namespace BakeryPos.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly ProductCatalogue _catalogue;
    private readonly Order _order = new();
    private string _otherHeading = "";

    public MainWindowViewModel(ProductCatalogue catalogue)
    {
        _catalogue = catalogue;

        Tabs =
        [
            new CategoryTabViewModel("Pies", Category.Pies, SelectCategory),
            new CategoryTabViewModel("Hot food", Category.HotFood, SelectCategory),
            new CategoryTabViewModel("Cakes", Category.Cakes, SelectCategory),
            new CategoryTabViewModel("Savoury", Category.Savoury, SelectCategory),
        ];

        SelectCategory(Category.Pies);
        RefreshOrder();
    }

    public IReadOnlyList<CategoryTabViewModel> Tabs { get; }

    public ObservableCollection<ProductTileViewModel> PopularTiles { get; } = new();

    public ObservableCollection<ProductTileViewModel> OtherTiles { get; } = new();

    public ObservableCollection<OrderLineViewModel> Lines { get; } = new();

    public bool HasPopular => PopularTiles.Count > 0;

    public string OtherHeading
    {
        get => _otherHeading;
        private set => Set(ref _otherHeading, value);
    }

    public string TotalText => Money.Format(_order.TotalCents);

    public bool IsOrderEmpty => _order.IsEmpty;

    private void SelectCategory(Category category)
    {
        foreach (var tab in Tabs)
        {
            tab.IsSelected = tab.Category == category;
        }

        PopularTiles.Clear();
        foreach (var product in _catalogue.Popular(category))
        {
            PopularTiles.Add(new ProductTileViewModel(product, AddProduct));
        }

        OtherTiles.Clear();
        foreach (var product in _catalogue.NotPopular(category))
        {
            OtherTiles.Add(new ProductTileViewModel(product, AddProduct));
        }

        OtherHeading = HasPopular ? "All other items" : "All items";
        Raise(nameof(HasPopular));
    }

    private void AddProduct(Product product)
    {
        _order.Add(product);
        RefreshOrder();
    }

    private void RefreshOrder()
    {
        Lines.Clear();
        foreach (var line in _order.Lines.ToList())
        {
            Lines.Add(new OrderLineViewModel(line, _order, RefreshOrder));
        }

        Raise(nameof(TotalText));
        Raise(nameof(IsOrderEmpty));
    }
}

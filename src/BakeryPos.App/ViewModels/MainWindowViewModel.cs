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
    private bool _isMiscellaneous;

    public MainWindowViewModel(ProductCatalogue catalogue)
    {
        _catalogue = catalogue;

        Tabs =
        [
            new CategoryTabViewModel("Pies", Category.Pies, () => SelectCategory(Category.Pies)),
            new CategoryTabViewModel("Hot food", Category.HotFood, () => SelectCategory(Category.HotFood)),
            new CategoryTabViewModel("Cakes", Category.Cakes, () => SelectCategory(Category.Cakes)),
            new CategoryTabViewModel("Savoury", Category.Savoury, () => SelectCategory(Category.Savoury)),
            new CategoryTabViewModel("Miscellaneous", null, SelectMiscellaneous),
        ];

        Misc = new MiscellaneousViewModel(AddMiscellaneous);

        SelectCategory(Category.Pies);
        RefreshOrder();
    }

    public IReadOnlyList<CategoryTabViewModel> Tabs { get; }

    public ObservableCollection<ProductTileViewModel> PopularTiles { get; } = new();

    public ObservableCollection<ProductTileViewModel> OtherTiles { get; } = new();

    public ObservableCollection<OrderLineViewModel> Lines { get; } = new();

    public MiscellaneousViewModel Misc { get; }

    public bool HasPopular => PopularTiles.Count > 0;

    /// <summary>True while the Miscellaneous keypad is showing instead of product tiles.</summary>
    public bool IsMiscellaneous
    {
        get => _isMiscellaneous;
        private set
        {
            if (Set(ref _isMiscellaneous, value))
            {
                Raise(nameof(ShowProducts));
            }
        }
    }

    public bool ShowProducts => !_isMiscellaneous;

    public string OtherHeading
    {
        get => _otherHeading;
        private set => Set(ref _otherHeading, value);
    }

    public string TotalText => Money.Format(_order.TotalCents);

    public bool IsOrderEmpty => _order.IsEmpty;

    private void SelectMiscellaneous()
    {
        foreach (var tab in Tabs)
        {
            tab.IsSelected = tab.Category is null;
        }

        Misc.Reset();
        IsMiscellaneous = true;
    }

    private void SelectCategory(Category category)
    {
        IsMiscellaneous = false;

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

    private void AddMiscellaneous(int priceCents)
    {
        _order.AddMiscellaneous(priceCents);
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

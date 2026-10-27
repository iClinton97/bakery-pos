using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BakeryPos.App.Payments;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;
using BakeryPos.Core.Payments;
using BakeryPos.Core.Sales;

namespace BakeryPos.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private enum Screen
    {
        Selling,
        CardWaiting,
        CardApproved,
        CardDeclined,
        CashPayment,
    }

    private readonly ProductCatalogue _catalogue;
    private readonly InteractiveMockTerminal _terminal;
    private readonly Checkout _checkout;
    private readonly Order _order = new();
    private readonly List<CompletedSale> _sales = new();

    private string _otherHeading = "";
    private bool _isMiscellaneous;
    private Screen _screen = Screen.Selling;
    private int _orderNumber = 1;
    private CompletedSale? _lastSale;

    public MainWindowViewModel(ProductCatalogue catalogue, InteractiveMockTerminal terminal)
    {
        _catalogue = catalogue;
        _terminal = terminal;
        _checkout = new Checkout(terminal);

        Tabs =
        [
            new CategoryTabViewModel("Pies", Category.Pies, () => SelectCategory(Category.Pies)),
            new CategoryTabViewModel("Hot food", Category.HotFood, () => SelectCategory(Category.HotFood)),
            new CategoryTabViewModel("Cakes", Category.Cakes, () => SelectCategory(Category.Cakes)),
            new CategoryTabViewModel("Savoury", Category.Savoury, () => SelectCategory(Category.Savoury)),
            new CategoryTabViewModel("Miscellaneous", null, SelectMiscellaneous),
        ];

        Misc = new MiscellaneousViewModel(AddMiscellaneous);
        Cash = new CashPaymentViewModel(() => _order.TotalCents, CompleteCashSale);

        PayByCardCommand = new RelayCommand(StartCardPayment);
        PayByCashCommand = new RelayCommand(OpenCash);
        VoidOrderCommand = new RelayCommand(VoidOrder);
        ApproveCommand = new RelayCommand(_terminal.Approve);
        DeclineCommand = new RelayCommand(_terminal.Decline);
        CancelCardCommand = new RelayCommand(_terminal.Cancel);
        BackToOrderCommand = new RelayCommand(() => Go(Screen.Selling));
        NewOrderCommand = new RelayCommand(StartNewOrder);

        SelectCategory(Category.Pies);
        RefreshOrder();
    }

    public IReadOnlyList<CategoryTabViewModel> Tabs { get; }

    public ObservableCollection<ProductTileViewModel> PopularTiles { get; } = new();

    public ObservableCollection<ProductTileViewModel> OtherTiles { get; } = new();

    public ObservableCollection<OrderLineViewModel> Lines { get; } = new();

    public MiscellaneousViewModel Misc { get; }

    public CashPaymentViewModel Cash { get; }

    public ICommand PayByCardCommand { get; }

    public ICommand PayByCashCommand { get; }

    public ICommand VoidOrderCommand { get; }

    public ICommand ApproveCommand { get; }

    public ICommand DeclineCommand { get; }

    public ICommand CancelCardCommand { get; }

    public ICommand BackToOrderCommand { get; }

    public ICommand NewOrderCommand { get; }

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

    public bool HasItems => !_order.IsEmpty;

    /// <summary>The order number as shown on screen and sent to the terminal, for example "0042".</summary>
    public string OrderNumberText => _orderNumber.ToString("D4");

    public bool IsCardWaiting => _screen == Screen.CardWaiting;

    public bool IsCardApproved => _screen == Screen.CardApproved;

    public bool IsCardDeclined => _screen == Screen.CardDeclined;

    public bool IsCashPayment => _screen == Screen.CashPayment;

    public string ApprovedTotalText => _lastSale is null ? "" : Money.Format(_lastSale.TotalCents);

    public string ApprovedSummary => _lastSale is null
        ? ""
        : $"{Money.Format(_lastSale.TotalCents)} by card · {_lastSale.CompletedAt.ToLocalTime():h:mm tt}";

    public string WaitingSummary => string.Join(", ", _order.Lines.Select(l => $"{l.Name} × {l.Quantity}"));

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

    private void VoidOrder()
    {
        _order.Clear();
        RefreshOrder();
    }

    private void StartCardPayment()
    {
        if (_order.IsEmpty || _screen == Screen.CardWaiting)
        {
            return;
        }

        _ = RunCardPaymentAsync();
    }

    private async Task RunCardPaymentAsync()
    {
        Go(Screen.CardWaiting);

        try
        {
            var result = await _checkout.PayByCardAsync(_order, OrderNumberText);

            if (result.Sale is { } sale)
            {
                _sales.Add(sale);
                _lastSale = sale;
                Go(Screen.CardApproved);
            }
            else if (result.Payment.Status == PaymentStatus.Declined)
            {
                Go(Screen.CardDeclined);
            }
            else
            {
                Go(Screen.Selling);
            }
        }
        catch (Exception)
        {
            Go(Screen.CardDeclined);
        }
    }

    private void OpenCash()
    {
        if (_order.IsEmpty)
        {
            return;
        }

        Cash.Reset();
        Go(Screen.CashPayment);
    }

    private void CompleteCashSale(int cashReceivedCents)
    {
        var sale = _checkout.PayWithCash(_order, OrderNumberText, cashReceivedCents);
        _sales.Add(sale);
        StartNewOrder();
    }

    private void StartNewOrder()
    {
        _order.Clear();
        _orderNumber++;
        RefreshOrder();
        Raise(nameof(OrderNumberText));
        Go(Screen.Selling);
    }

    private void Go(Screen screen)
    {
        _screen = screen;
        Raise(nameof(IsCardWaiting));
        Raise(nameof(IsCardApproved));
        Raise(nameof(IsCardDeclined));
        Raise(nameof(IsCashPayment));
        Raise(nameof(ApprovedTotalText));
        Raise(nameof(ApprovedSummary));
        Raise(nameof(WaitingSummary));
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
        Raise(nameof(HasItems));
        Raise(nameof(WaitingSummary));
    }
}

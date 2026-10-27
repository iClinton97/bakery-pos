using System;
using System.Collections.Generic;
using System.Windows.Input;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;

namespace BakeryPos.App.ViewModels;

/// <summary>The cash screen: how much the customer handed over, and the change due.</summary>
public sealed class CashPaymentViewModel : ViewModelBase
{
    private readonly PriceEntry _entry = new();
    private readonly Func<int> _totalCents;
    private readonly Action<int> _complete;
    private readonly Action<int> _split;

    public CashPaymentViewModel(Func<int> totalCents, Action<int> complete, Action<int> split)
    {
        _totalCents = totalCents;
        _complete = complete;
        _split = split;

        var keys = new List<KeypadKeyViewModel>();
        foreach (var label in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0" })
        {
            keys.Add(new KeypadKeyViewModel(label, () => Press(label[0])));
        }

        keys.Add(new KeypadKeyViewModel("Del", Backspace));
        Keys = keys;

        QuickAmounts =
        [
            new KeypadKeyViewModel("Exact", () => SetCents(_totalCents())),
            new KeypadKeyViewModel("$20", () => SetCents(2000)),
            new KeypadKeyViewModel("$50", () => SetCents(5000)),
            new KeypadKeyViewModel("$100", () => SetCents(10000)),
        ];

        CompleteCommand = new RelayCommand(Complete);
        SplitCommand = new RelayCommand(Split);
    }

    public IReadOnlyList<KeypadKeyViewModel> Keys { get; }

    public IReadOnlyList<KeypadKeyViewModel> QuickAmounts { get; }

    public ICommand CompleteCommand { get; }

    /// <summary>Takes the cash typed so far and charges the card for the rest.</summary>
    public ICommand SplitCommand { get; }

    public string DueText => Money.Format(_totalCents());

    public string ReceivedText => _entry.DisplayText;

    /// <summary>"Change due" once enough is received, otherwise "Still to pay".</summary>
    public string ChangeLabel => HasEnough ? "Change due" : "Still to pay";

    public string ChangeText => Money.Format(Math.Abs(_entry.Cents - _totalCents()));

    public bool CanComplete => HasEnough;

    /// <summary>A split needs some cash, but less than the total.</summary>
    public bool CanSplit => _entry.Cents > 0 && _entry.Cents < _totalCents();

    private bool HasEnough => _totalCents() > 0 && _entry.Cents >= _totalCents();

    public void Reset()
    {
        _entry.Clear();
        Refresh();
    }

    private void Press(char key)
    {
        _entry.Press(key);
        Refresh();
    }

    private void Backspace()
    {
        _entry.Backspace();
        Refresh();
    }

    private void SetCents(int cents)
    {
        _entry.SetCents(cents);
        Refresh();
    }

    private void Complete()
    {
        if (CanComplete)
        {
            _complete(_entry.Cents);
        }
    }

    private void Split()
    {
        if (CanSplit)
        {
            _split(_entry.Cents);
        }
    }

    private void Refresh()
    {
        Raise(nameof(DueText));
        Raise(nameof(ReceivedText));
        Raise(nameof(ChangeLabel));
        Raise(nameof(ChangeText));
        Raise(nameof(CanComplete));
        Raise(nameof(CanSplit));
    }
}

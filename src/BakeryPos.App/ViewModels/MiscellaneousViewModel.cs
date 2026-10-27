using System;
using System.Collections.Generic;
using System.Windows.Input;
using BakeryPos.Core.Orders;

namespace BakeryPos.App.ViewModels;

/// <summary>The Miscellaneous screen: a keypad for typing a price that is not on any button.</summary>
public sealed class MiscellaneousViewModel : ViewModelBase
{
    private readonly PriceEntry _entry = new();
    private readonly Action<int> _addToOrder;

    public MiscellaneousViewModel(Action<int> addToOrder)
    {
        _addToOrder = addToOrder;

        var keys = new List<KeypadKeyViewModel>();
        foreach (var label in new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0" })
        {
            keys.Add(new KeypadKeyViewModel(label, () => Press(label[0])));
        }

        keys.Add(new KeypadKeyViewModel("Del", Backspace));
        Keys = keys;

        AddCommand = new RelayCommand(Add);
    }

    public IReadOnlyList<KeypadKeyViewModel> Keys { get; }

    public ICommand AddCommand { get; }

    public string DisplayText => _entry.DisplayText;

    public bool CanAdd => _entry.Cents > 0;

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

    private void Add()
    {
        if (!CanAdd)
        {
            return;
        }

        _addToOrder(_entry.Cents);
        Reset();
    }

    private void Refresh()
    {
        Raise(nameof(DisplayText));
        Raise(nameof(CanAdd));
    }
}

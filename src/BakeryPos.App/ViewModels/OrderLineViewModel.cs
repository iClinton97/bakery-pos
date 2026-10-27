using System;
using System.Windows.Input;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;

namespace BakeryPos.App.ViewModels;

/// <summary>One row in the current order, with its plus, minus and remove buttons.</summary>
public sealed class OrderLineViewModel
{
    public OrderLineViewModel(OrderLine line, Order order, Action changed)
    {
        Name = line.Name;
        Quantity = line.Quantity;
        LineTotalText = Money.Format(line.LineTotalCents);
        IncreaseCommand = new RelayCommand(() => { order.Increase(line.Key); changed(); });
        DecreaseCommand = new RelayCommand(() => { order.Decrease(line.Key); changed(); });
        RemoveCommand = new RelayCommand(() => { order.Remove(line.Key); changed(); });
    }

    public string Name { get; }

    public int Quantity { get; }

    public string LineTotalText { get; }

    public ICommand IncreaseCommand { get; }

    public ICommand DecreaseCommand { get; }

    public ICommand RemoveCommand { get; }
}

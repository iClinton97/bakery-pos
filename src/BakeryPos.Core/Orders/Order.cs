using System;
using System.Collections.Generic;
using System.Linq;
using BakeryPos.Core.Catalogue;

namespace BakeryPos.Core.Orders;

/// <summary>
/// The sale being built at the till. This class only does the arithmetic;
/// payment and stock changes happen elsewhere, once a payment is approved.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = new();
    private int _nextMiscNumber = 1;

    public IReadOnlyList<OrderLine> Lines => _lines;

    public bool IsEmpty => _lines.Count == 0;

    public int TotalCents => _lines.Sum(l => l.LineTotalCents);

    /// <summary>Adds one of the product, or one more if it is already on the order.</summary>
    public void Add(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existing = _lines.Find(l => l.Key == product.Id);
        if (existing is null)
        {
            _lines.Add(new OrderLine(product.Id, product.Name, product.PriceCents, product));
        }
        else
        {
            existing.Quantity++;
        }
    }

    /// <summary>Adds a manual price. Each miscellaneous item is its own line.</summary>
    public void AddMiscellaneous(int priceCents)
    {
        if (priceCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priceCents), "Price must be greater than zero.");
        }

        _lines.Add(new OrderLine($"misc-{_nextMiscNumber++}", "Miscellaneous", priceCents, product: null));
    }

    public void Increase(string key) => Get(key).Quantity++;

    /// <summary>Removes one. The line disappears when it reaches zero.</summary>
    public void Decrease(string key)
    {
        var line = Get(key);
        if (line.Quantity <= 1)
        {
            _lines.Remove(line);
        }
        else
        {
            line.Quantity--;
        }
    }

    public void Remove(string key) => _lines.Remove(Get(key));

    public void Clear() => _lines.Clear();

    private OrderLine Get(string key) =>
        _lines.Find(l => l.Key == key) ?? throw new ArgumentException($"No line '{key}' on this order.", nameof(key));
}

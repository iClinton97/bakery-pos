using BakeryPos.Core.Catalogue;

namespace BakeryPos.Core.Orders;

/// <summary>One row on the order. A line is either a catalogue product or a miscellaneous manual price.</summary>
public sealed class OrderLine
{
    internal OrderLine(string key, string name, int unitPriceCents, Product? product)
    {
        Key = key;
        Name = name;
        UnitPriceCents = unitPriceCents;
        Product = product;
        Quantity = 1;
    }

    /// <summary>Identifies the line within its order. Same as the product id for products.</summary>
    public string Key { get; }

    public string Name { get; }

    public int UnitPriceCents { get; }

    public int Quantity { get; internal set; }

    /// <summary>Null for miscellaneous lines, which have no product and are not tracked in stock.</summary>
    public Product? Product { get; }

    public bool IsMiscellaneous => Product is null;

    public int LineTotalCents => checked(UnitPriceCents * Quantity);
}

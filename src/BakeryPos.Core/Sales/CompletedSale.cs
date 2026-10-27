using System;
using System.Collections.Generic;

namespace BakeryPos.Core.Sales;

public enum PaymentMethod
{
    Card,
    Cash,
}

/// <summary>A copy of one order line, frozen at the moment of sale.</summary>
public sealed record SaleLine(string Name, int UnitPriceCents, int Quantity, string? ProductId)
{
    public int LineTotalCents => checked(UnitPriceCents * Quantity);

    /// <summary>Miscellaneous lines have no product, so they never affect stock.</summary>
    public bool IsMiscellaneous => ProductId is null;
}

/// <summary>A finished, paid sale. Nothing here changes after it is created.</summary>
public sealed record CompletedSale(
    string Reference,
    IReadOnlyList<SaleLine> Lines,
    int TotalCents,
    PaymentMethod Method,
    DateTimeOffset CompletedAt,
    int CashReceivedCents = 0)
{
    /// <summary>Change handed back. Always zero for card sales.</summary>
    public int ChangeCents => Method == PaymentMethod.Cash ? CashReceivedCents - TotalCents : 0;
}

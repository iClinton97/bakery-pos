using System;
using System.Collections.Generic;
using System.Linq;

namespace BakeryPos.Core.Sales;

public enum PaymentMethod
{
    Card,
    Cash,

    /// <summary>Part cash and part card. Only used to describe a whole sale, never a single payment.</summary>
    Split,
}

/// <summary>A copy of one order line, frozen at the moment of sale.</summary>
public sealed record SaleLine(string Name, int UnitPriceCents, int Quantity, string? ProductId)
{
    public int LineTotalCents => checked(UnitPriceCents * Quantity);

    /// <summary>Miscellaneous lines have no product, so they never affect stock.</summary>
    public bool IsMiscellaneous => ProductId is null;
}

/// <summary>One part of how a sale was paid. For cash, the amount counts towards the sale, so change is not included.</summary>
public sealed record SalePayment(PaymentMethod Method, int AmountCents);

/// <summary>A finished, paid sale. Nothing here changes after it is created.</summary>
public sealed record CompletedSale(
    string Reference,
    IReadOnlyList<SaleLine> Lines,
    int TotalCents,
    IReadOnlyList<SalePayment> Payments,
    DateTimeOffset CompletedAt,
    int CashReceivedCents = 0)
{
    public int CashCents => Payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.AmountCents);

    public int CardCents => Payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.AmountCents);

    /// <summary>Card, Cash, or Split when the sale was paid with both.</summary>
    public PaymentMethod Method => Payments.Count > 1 ? PaymentMethod.Split : Payments[0].Method;

    /// <summary>Change handed back. Only a full cash sale can have change.</summary>
    public int ChangeCents => Method == PaymentMethod.Cash ? CashReceivedCents - TotalCents : 0;
}

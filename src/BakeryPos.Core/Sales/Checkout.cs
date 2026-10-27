using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BakeryPos.Core.Orders;
using BakeryPos.Core.Payments;

namespace BakeryPos.Core.Sales;

/// <summary>What happened when a card payment was attempted.</summary>
/// <param name="Payment">The terminal's answer.</param>
/// <param name="Sale">The finished sale, or null if the payment was not approved.</param>
public sealed record CardCheckoutResult(PaymentResult Payment, CompletedSale? Sale);

/// <summary>
/// Turns an order into a finished sale. A sale only exists once the whole total is paid:
/// an approved card payment, enough cash, or cash plus an approved card for the remainder.
/// A declined or cancelled card gives no sale.
/// </summary>
public sealed class Checkout
{
    private readonly IPaymentTerminal _terminal;
    private readonly TimeProvider _clock;

    public Checkout(IPaymentTerminal terminal, TimeProvider? clock = null)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<CardCheckoutResult> PayByCardAsync(
        Order order,
        string reference,
        CancellationToken cancellationToken = default)
    {
        RequireItems(order);

        var payment = await _terminal.ChargeAsync(
            new PaymentRequest(order.TotalCents, reference),
            cancellationToken);

        var sale = payment.IsApproved
            ? Snapshot(order, reference, [new SalePayment(PaymentMethod.Card, order.TotalCents)], cashReceivedCents: 0)
            : null;

        return new CardCheckoutResult(payment, sale);
    }

    /// <summary>
    /// The customer pays part in cash and the rest by card. The card is charged only for
    /// the remainder. The sale exists only if the card is approved.
    /// </summary>
    public async Task<CardCheckoutResult> PayBySplitAsync(
        Order order,
        string reference,
        int cashCents,
        CancellationToken cancellationToken = default)
    {
        RequireItems(order);

        if (cashCents <= 0 || cashCents >= order.TotalCents)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cashCents),
                "The cash part must be more than zero and less than the total.");
        }

        var cardCents = order.TotalCents - cashCents;

        var payment = await _terminal.ChargeAsync(
            new PaymentRequest(cardCents, reference),
            cancellationToken);

        var sale = payment.IsApproved
            ? Snapshot(
                order,
                reference,
                [new SalePayment(PaymentMethod.Cash, cashCents), new SalePayment(PaymentMethod.Card, cardCents)],
                cashReceivedCents: cashCents)
            : null;

        return new CardCheckoutResult(payment, sale);
    }

    public CompletedSale PayWithCash(Order order, string reference, int cashReceivedCents)
    {
        RequireItems(order);

        if (cashReceivedCents < order.TotalCents)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cashReceivedCents),
                "Cash received must cover the total.");
        }

        return Snapshot(order, reference, [new SalePayment(PaymentMethod.Cash, order.TotalCents)], cashReceivedCents);
    }

    private static void RequireItems(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.IsEmpty)
        {
            throw new InvalidOperationException("The order is empty.");
        }
    }

    private CompletedSale Snapshot(
        Order order,
        string reference,
        IReadOnlyList<SalePayment> payments,
        int cashReceivedCents)
    {
        var lines = order.Lines
            .Select(l => new SaleLine(l.Name, l.UnitPriceCents, l.Quantity, l.Product?.Id))
            .ToList();

        return new CompletedSale(
            reference,
            lines,
            order.TotalCents,
            payments,
            _clock.GetUtcNow(),
            cashReceivedCents);
    }
}

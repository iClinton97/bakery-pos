using System;
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
/// Turns an order into a finished sale. A sale only exists once money has been taken:
/// an approved card payment, or enough cash. A declined or cancelled card gives no sale.
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
            ? Snapshot(order, reference, PaymentMethod.Card, cashReceivedCents: 0)
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

        return Snapshot(order, reference, PaymentMethod.Cash, cashReceivedCents);
    }

    private static void RequireItems(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.IsEmpty)
        {
            throw new InvalidOperationException("The order is empty.");
        }
    }

    private CompletedSale Snapshot(Order order, string reference, PaymentMethod method, int cashReceivedCents)
    {
        var lines = order.Lines
            .Select(l => new SaleLine(l.Name, l.UnitPriceCents, l.Quantity, l.Product?.Id))
            .ToList();

        return new CompletedSale(
            reference,
            lines,
            order.TotalCents,
            method,
            _clock.GetUtcNow(),
            cashReceivedCents);
    }
}

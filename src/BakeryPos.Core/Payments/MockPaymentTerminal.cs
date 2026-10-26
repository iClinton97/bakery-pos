using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BakeryPos.Core.Payments;

/// <summary>
/// A fake terminal for development, tests and demos. Set <see cref="NextStatus"/>
/// to simulate an approved, declined or cancelled payment.
/// </summary>
public sealed class MockPaymentTerminal : IPaymentTerminal
{
    private readonly List<PaymentRequest> _requests = new();

    public PaymentStatus NextStatus { get; set; } = PaymentStatus.Approved;

    public IReadOnlyList<PaymentRequest> Requests => _requests;

    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AmountCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Amount must be greater than zero.");
        }

        _requests.Add(request);

        var message = NextStatus switch
        {
            PaymentStatus.Approved => "Approved (mock)",
            PaymentStatus.Declined => "Declined (mock)",
            _ => "Cancelled (mock)",
        };

        return Task.FromResult(new PaymentResult(NextStatus, message));
    }
}

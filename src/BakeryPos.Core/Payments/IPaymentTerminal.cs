using System.Threading;
using System.Threading.Tasks;

namespace BakeryPos.Core.Payments;

public enum PaymentStatus
{
    Approved,
    Declined,
    Cancelled,
}

/// <summary>A request to charge a card. Amounts are in cents to avoid floating point errors.</summary>
public sealed record PaymentRequest(int AmountCents, string Reference);

public sealed record PaymentResult(PaymentStatus Status, string? Message = null)
{
    public bool IsApproved => Status == PaymentStatus.Approved;
}

/// <summary>
/// The only way the app talks to an EFTPOS terminal. The real Tyro implementation
/// and the mock used for development both implement this.
/// </summary>
public interface IPaymentTerminal
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}

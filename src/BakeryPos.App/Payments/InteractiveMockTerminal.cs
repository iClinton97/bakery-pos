using System.Threading;
using System.Threading.Tasks;
using BakeryPos.Core.Payments;

namespace BakeryPos.App.Payments;

/// <summary>
/// A pretend EFTPOS terminal for development. A charge stays "waiting" until someone
/// presses Approve, Decline or Cancel, like a customer deciding at the real terminal.
/// </summary>
public sealed class InteractiveMockTerminal : IPaymentTerminal
{
    private TaskCompletionSource<PaymentResult>? _pending;

    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var pending = new TaskCompletionSource<PaymentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = pending;
        return pending.Task;
    }

    public void Approve() => Complete(PaymentStatus.Approved, "Approved (mock)");

    public void Decline() => Complete(PaymentStatus.Declined, "Declined (mock)");

    public void Cancel() => Complete(PaymentStatus.Cancelled, "Cancelled (mock)");

    private void Complete(PaymentStatus status, string message)
    {
        var pending = _pending;
        _pending = null;
        pending?.TrySetResult(new PaymentResult(status, message));
    }
}

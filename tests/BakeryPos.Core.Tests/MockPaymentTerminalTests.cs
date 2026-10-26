using System;
using System.Threading.Tasks;
using BakeryPos.Core.Payments;
using Xunit;

namespace BakeryPos.Core.Tests;

public class MockPaymentTerminalTests
{
    [Fact]
    public async Task Charge_IsApproved_ByDefault()
    {
        var terminal = new MockPaymentTerminal();

        var result = await terminal.ChargeAsync(new PaymentRequest(2730, "order-1"));

        Assert.True(result.IsApproved);
        Assert.Single(terminal.Requests);
    }

    [Fact]
    public async Task Charge_IsDeclined_WhenConfigured()
    {
        var terminal = new MockPaymentTerminal { NextStatus = PaymentStatus.Declined };

        var result = await terminal.ChargeAsync(new PaymentRequest(500, "order-2"));

        Assert.False(result.IsApproved);
        Assert.Equal(PaymentStatus.Declined, result.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Charge_Throws_ForNonPositiveAmount(int cents)
    {
        var terminal = new MockPaymentTerminal();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => terminal.ChargeAsync(new PaymentRequest(cents, "order-3")));
    }
}

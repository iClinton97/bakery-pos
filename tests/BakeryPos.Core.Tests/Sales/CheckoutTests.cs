using System;
using System.Threading.Tasks;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Orders;
using BakeryPos.Core.Payments;
using BakeryPos.Core.Sales;
using Xunit;

namespace BakeryPos.Core.Tests.Sales;

public class CheckoutTests
{
    private static readonly Product Pie = new("Plain Pies", Category.Pies, 550, 36, 8);
    private static readonly Product Dimmy = new("Large dim sim", Category.HotFood, 250, 40, 10);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static Order OrderWithPieAndMisc()
    {
        var order = new Order();
        order.Add(Pie);
        order.Add(Pie);
        order.AddMiscellaneous(350);
        return order;
    }

    [Fact]
    public async Task Card_Approved_ReturnsASale_WithTheOrderDetails()
    {
        var now = new DateTimeOffset(2026, 10, 27, 10, 42, 0, TimeSpan.Zero);
        var terminal = new MockPaymentTerminal();
        var checkout = new Checkout(terminal, new FixedClock(now));

        var result = await checkout.PayByCardAsync(OrderWithPieAndMisc(), "0042");

        Assert.True(result.Payment.IsApproved);
        var sale = Assert.IsType<CompletedSale>(result.Sale);
        Assert.Equal("0042", sale.Reference);
        Assert.Equal(1450, sale.TotalCents);
        Assert.Equal(PaymentMethod.Card, sale.Method);
        Assert.Equal(now, sale.CompletedAt);
        Assert.Equal(2, sale.Lines.Count);
        Assert.Equal(0, sale.ChangeCents);
    }

    [Fact]
    public async Task Card_SendsTheTotalAndReference_ToTheTerminal()
    {
        var terminal = new MockPaymentTerminal();
        var checkout = new Checkout(terminal);

        await checkout.PayByCardAsync(OrderWithPieAndMisc(), "0042");

        var request = Assert.Single(terminal.Requests);
        Assert.Equal(1450, request.AmountCents);
        Assert.Equal("0042", request.Reference);
    }

    [Theory]
    [InlineData(PaymentStatus.Declined)]
    [InlineData(PaymentStatus.Cancelled)]
    public async Task Card_NotApproved_GivesNoSale(PaymentStatus status)
    {
        var terminal = new MockPaymentTerminal { NextStatus = status };
        var checkout = new Checkout(terminal);

        var result = await checkout.PayByCardAsync(OrderWithPieAndMisc(), "0042");

        Assert.False(result.Payment.IsApproved);
        Assert.Null(result.Sale);
    }

    [Fact]
    public async Task Card_EmptyOrder_Throws_AndNeverCallsTheTerminal()
    {
        var terminal = new MockPaymentTerminal();
        var checkout = new Checkout(terminal);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => checkout.PayByCardAsync(new Order(), "0042"));

        Assert.Empty(terminal.Requests);
    }

    [Fact]
    public void Cash_ExactAmount_HasNoChange()
    {
        var checkout = new Checkout(new MockPaymentTerminal());

        var sale = checkout.PayWithCash(OrderWithPieAndMisc(), "0043", 1450);

        Assert.Equal(PaymentMethod.Cash, sale.Method);
        Assert.Equal(0, sale.ChangeCents);
    }

    [Fact]
    public void Cash_Overpaid_ReturnsChange()
    {
        var checkout = new Checkout(new MockPaymentTerminal());

        var sale = checkout.PayWithCash(OrderWithPieAndMisc(), "0043", 2000);

        Assert.Equal(550, sale.ChangeCents);
        Assert.Equal(2000, sale.CashReceivedCents);
    }

    [Fact]
    public void Cash_Short_Throws()
    {
        var checkout = new Checkout(new MockPaymentTerminal());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => checkout.PayWithCash(OrderWithPieAndMisc(), "0043", 1449));
    }

    [Fact]
    public void Cash_EmptyOrder_Throws()
    {
        var checkout = new Checkout(new MockPaymentTerminal());

        Assert.Throws<InvalidOperationException>(() => checkout.PayWithCash(new Order(), "0043", 1000));
    }

    [Fact]
    public void Sale_IsASnapshot_ChangesToTheOrderDoNotAffectIt()
    {
        var checkout = new Checkout(new MockPaymentTerminal());
        var order = OrderWithPieAndMisc();

        var sale = checkout.PayWithCash(order, "0043", 2000);
        order.Add(Dimmy);
        order.Clear();

        Assert.Equal(2, sale.Lines.Count);
        Assert.Equal(1450, sale.TotalCents);
    }

    [Fact]
    public void Sale_MarksMiscellaneousLines_ByHavingNoProductId()
    {
        var checkout = new Checkout(new MockPaymentTerminal());

        var sale = checkout.PayWithCash(OrderWithPieAndMisc(), "0043", 2000);

        Assert.Contains(sale.Lines, l => !l.IsMiscellaneous && l.ProductId == "plain-pies");
        Assert.Contains(sale.Lines, l => l.IsMiscellaneous);
    }
}

using System;
using BakeryPos.Core.Security;
using Xunit;

namespace BakeryPos.Core.Tests.Security;

public class SessionStoreTests
{
    [Fact]
    public void ANewSession_IsValid()
    {
        var store = new SessionStore();

        Assert.True(store.IsValid(store.Create()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-real-token")]
    public void UnknownTokens_AreNotValid(string? token)
    {
        Assert.False(new SessionStore().IsValid(token));
    }

    [Fact]
    public void Sessions_ExpireAfterTheirLifetime()
    {
        var clock = new TestClock();
        var store = new SessionStore(clock, TimeSpan.FromHours(1));
        var token = store.Create();

        clock.Advance(TimeSpan.FromMinutes(59));
        Assert.True(store.IsValid(token));

        clock.Advance(TimeSpan.FromMinutes(2));
        Assert.False(store.IsValid(token));
    }

    [Fact]
    public void Revoke_EndsASession()
    {
        var store = new SessionStore();
        var token = store.Create();

        store.Revoke(token);

        Assert.False(store.IsValid(token));
    }

    [Fact]
    public void Tokens_AreLongAndUnique()
    {
        var store = new SessionStore();

        var first = store.Create();
        var second = store.Create();

        Assert.NotEqual(first, second);
        Assert.True(first.Length >= 32);
    }
}

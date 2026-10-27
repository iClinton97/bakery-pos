using System;
using BakeryPos.Core.Security;
using Xunit;

namespace BakeryPos.Core.Tests.Security;

public class LoginGuardTests
{
    [Fact]
    public void RightPin_Succeeds()
    {
        var guard = new LoginGuard("482913");

        Assert.Equal(LoginResult.Success, guard.TryLogin("tablet", "482913"));
    }

    [Theory]
    [InlineData("000000")]
    [InlineData("")]
    [InlineData(null)]
    public void WrongPin_IsRefused(string? attempt)
    {
        var guard = new LoginGuard("482913");

        Assert.Equal(LoginResult.WrongPin, guard.TryLogin("tablet", attempt));
    }

    [Fact]
    public void TooManyWrongTries_LockTheDeviceOut()
    {
        var guard = new LoginGuard("482913", maxFailures: 3);

        Assert.Equal(LoginResult.WrongPin, guard.TryLogin("tablet", "1"));
        Assert.Equal(LoginResult.WrongPin, guard.TryLogin("tablet", "2"));
        Assert.Equal(LoginResult.LockedOut, guard.TryLogin("tablet", "3"));
    }

    [Fact]
    public void WhileLockedOut_EvenTheRightPinIsRefused()
    {
        var guard = new LoginGuard("482913", maxFailures: 2);
        guard.TryLogin("tablet", "1");
        guard.TryLogin("tablet", "2");

        Assert.Equal(LoginResult.LockedOut, guard.TryLogin("tablet", "482913"));
    }

    [Fact]
    public void TheLockout_EndsAfterTheSetTime()
    {
        var clock = new TestClock();
        var guard = new LoginGuard("482913", clock, maxFailures: 2, lockout: TimeSpan.FromMinutes(5));
        guard.TryLogin("tablet", "1");
        guard.TryLogin("tablet", "2");

        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal(LoginResult.Success, guard.TryLogin("tablet", "482913"));
    }

    [Fact]
    public void RemainingLockout_CountsDown()
    {
        var clock = new TestClock();
        var guard = new LoginGuard("482913", clock, maxFailures: 1, lockout: TimeSpan.FromMinutes(5));
        guard.TryLogin("tablet", "1");

        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(TimeSpan.FromMinutes(3), guard.RemainingLockout("tablet"));
        Assert.Equal(TimeSpan.Zero, guard.RemainingLockout("someone-else"));
    }

    [Fact]
    public void ASuccessfulLogin_ResetsTheFailureCount()
    {
        var guard = new LoginGuard("482913", maxFailures: 3);
        guard.TryLogin("tablet", "1");
        guard.TryLogin("tablet", "2");
        guard.TryLogin("tablet", "482913");

        Assert.Equal(LoginResult.WrongPin, guard.TryLogin("tablet", "1"));
        Assert.Equal(LoginResult.WrongPin, guard.TryLogin("tablet", "2"));
    }

    [Fact]
    public void Devices_AreLockedOutSeparately()
    {
        var guard = new LoginGuard("482913", maxFailures: 1);
        guard.TryLogin("phone", "1");

        Assert.Equal(LoginResult.Success, guard.TryLogin("tablet", "482913"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    public void ShortPins_AreRejected(string pin)
    {
        Assert.Throws<ArgumentException>(() => new LoginGuard(pin));
    }
}

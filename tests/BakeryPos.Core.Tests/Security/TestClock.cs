using System;

namespace BakeryPos.Core.Tests.Security;

/// <summary>A clock the tests can move forward by hand.</summary>
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan time) => _now += time;
}

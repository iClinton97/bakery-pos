using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BakeryPos.Core.Security;

public enum LoginResult
{
    Success,
    WrongPin,

    /// <summary>Too many wrong tries. Even the right PIN is refused until the lock-out ends.</summary>
    LockedOut,
}

/// <summary>
/// Checks a PIN and stops guessing: after a few wrong tries from one device, that device is locked out
/// for a while, even if it then types the right PIN.
/// </summary>
public sealed class LoginGuard
{
    private sealed class Attempts
    {
        public int Failures;
        public DateTimeOffset? LockedUntil;
    }

    private readonly byte[] _pin;
    private readonly TimeProvider _clock;
    private readonly int _maxFailures;
    private readonly TimeSpan _lockout;
    private readonly Dictionary<string, Attempts> _byClient = new();
    private readonly object _gate = new();

    public LoginGuard(string pin, TimeProvider? clock = null, int maxFailures = 5, TimeSpan? lockout = null)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < 4)
        {
            throw new ArgumentException("The PIN must be at least 4 characters.", nameof(pin));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maxFailures, 1);

        _pin = Encoding.UTF8.GetBytes(pin);
        _clock = clock ?? TimeProvider.System;
        _maxFailures = maxFailures;
        _lockout = lockout ?? TimeSpan.FromMinutes(5);
    }

    /// <param name="clientId">Who is trying, for example the device's network address.</param>
    public LoginResult TryLogin(string clientId, string? attempt)
    {
        ArgumentNullException.ThrowIfNull(clientId);

        lock (_gate)
        {
            var now = _clock.GetUtcNow();

            if (!_byClient.TryGetValue(clientId, out var state))
            {
                state = new Attempts();
                _byClient[clientId] = state;
            }

            if (state.LockedUntil is { } until)
            {
                if (now < until)
                {
                    return LoginResult.LockedOut;
                }

                state.LockedUntil = null;
                state.Failures = 0;
            }

            var supplied = Encoding.UTF8.GetBytes(attempt ?? "");
            if (CryptographicOperations.FixedTimeEquals(supplied, _pin))
            {
                state.Failures = 0;
                return LoginResult.Success;
            }

            state.Failures++;
            if (state.Failures >= _maxFailures)
            {
                state.LockedUntil = now + _lockout;
                return LoginResult.LockedOut;
            }

            return LoginResult.WrongPin;
        }
    }

    /// <summary>How much longer this device is locked out, or zero if it is not.</summary>
    public TimeSpan RemainingLockout(string clientId)
    {
        lock (_gate)
        {
            if (_byClient.TryGetValue(clientId, out var state) && state.LockedUntil is { } until)
            {
                var left = until - _clock.GetUtcNow();
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }

            return TimeSpan.Zero;
        }
    }
}

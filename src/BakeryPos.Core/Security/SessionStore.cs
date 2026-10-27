using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace BakeryPos.Core.Security;

/// <summary>
/// Remembers who has entered the PIN. A session is a long random token that the browser keeps,
/// and it stops working after a set time.
/// </summary>
public sealed class SessionStore
{
    private readonly TimeProvider _clock;
    private readonly TimeSpan _lifetime;
    private readonly Dictionary<string, DateTimeOffset> _expiry = new();
    private readonly object _gate = new();

    public SessionStore(TimeProvider? clock = null, TimeSpan? lifetime = null)
    {
        _clock = clock ?? TimeProvider.System;
        _lifetime = lifetime ?? TimeSpan.FromHours(8);
    }

    public string Create()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        lock (_gate)
        {
            var now = _clock.GetUtcNow();
            foreach (var old in _expiry.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList())
            {
                _expiry.Remove(old);
            }

            _expiry[token] = now + _lifetime;
        }

        return token;
    }

    public bool IsValid(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        lock (_gate)
        {
            return _expiry.TryGetValue(token, out var expires) && expires > _clock.GetUtcNow();
        }
    }

    public void Revoke(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        lock (_gate)
        {
            _expiry.Remove(token);
        }
    }
}

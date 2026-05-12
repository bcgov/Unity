using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Unity.GrantManager.Web.Middleware;

/// <summary>
/// Singleton that tracks per-exception-type cooldowns and a global rate limit
/// to prevent Teams notification storms during an outage.
/// </summary>
public sealed class ExceptionNotificationThrottle
{
    // Only send one notification per exception type per cooldown window
    private static readonly TimeSpan PerTypeCooldown = TimeSpan.FromMinutes(5);

    // Global cap: at most N notifications per rolling minute across all types
    private const int GlobalMaxPerMinute = 5;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSent = new();

    // _sentThisMinute and _windowStart are always accessed together under _lock
    private readonly object _lock = new();
    private int _sentThisMinute;
    private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns true if a Teams notification should be sent for this exception type.
    /// Thread-safe.
    /// </summary>
    public bool ShouldNotify(string exceptionTypeName)
    {
        var now = DateTimeOffset.UtcNow;

        // Per-type cooldown check — ConcurrentDictionary read is lock-free
        if (_lastSent.TryGetValue(exceptionTypeName, out var last) &&
            now - last < PerTypeCooldown)
        {
            return false;
        }

        lock (_lock)
        {
            // Reset the window if a full minute has elapsed
            if (now - _windowStart >= TimeSpan.FromMinutes(1))
            {
                _sentThisMinute = 0;
                _windowStart = now;
            }

            if (_sentThisMinute >= GlobalMaxPerMinute)
            {
                return false;
            }

            _sentThisMinute++;
        }

        _lastSent[exceptionTypeName] = now;
        return true;
    }
}

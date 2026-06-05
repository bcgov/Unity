using System;
using System.Collections.Generic;

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

    // All state is accessed exclusively under _lock — no concurrent collections needed
    private readonly object _lock = new();
    private readonly Dictionary<string, DateTimeOffset> _lastSent = new();
    private int _sentThisMinute;
    private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns true if a Teams notification should be sent for this exception type.
    /// Thread-safe.
    /// </summary>
    public bool ShouldNotify(string exceptionTypeName)
    {
        var now = DateTimeOffset.UtcNow;

        lock (_lock)
        {
            // Reset the global window if a full minute has elapsed
            if (now - _windowStart >= TimeSpan.FromMinutes(1))
            {
                _sentThisMinute = 0;
                _windowStart = now;
            }

            // Per-type cooldown check — inside the lock to prevent concurrent
            // callers with the same exception type both passing the check
            if (_lastSent.TryGetValue(exceptionTypeName, out var last) &&
                now - last < PerTypeCooldown)
            {
                return false;
            }

            if (_sentThisMinute >= GlobalMaxPerMinute)
            {
                return false;
            }

            _sentThisMinute++;
            _lastSent[exceptionTypeName] = now;
            return true;
        }
    }
}

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
    private int _sentThisMinute;
    private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns true if a Teams notification should be sent for this exception type.
    /// Thread-safe.
    /// </summary>
    public bool ShouldNotify(string exceptionTypeName)
    {
        ResetWindowIfNeeded();

        if (_sentThisMinute >= GlobalMaxPerMinute)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;

        if (_lastSent.TryGetValue(exceptionTypeName, out var last) &&
            now - last < PerTypeCooldown)
        {
            return false;
        }

        _lastSent[exceptionTypeName] = now;
        Interlocked.Increment(ref _sentThisMinute);
        return true;
    }

    private void ResetWindowIfNeeded()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _windowStart >= TimeSpan.FromMinutes(1))
        {
            Interlocked.Exchange(ref _sentThisMinute, 0);
            _windowStart = now;
        }
    }
}

using System;
using System.Threading;

namespace Unity.Modules.Shared.Utils;

/// <summary>
/// Context marker for background job execution that survives async boundaries.
/// Used by auditing overrides to detect when code is running in a background job.
/// </summary>
public static class BackgroundJobExecutionContext
{
    private static readonly AsyncLocal<bool> _isActive = new();

    /// <summary>
    /// Returns true if currently executing within a background job context.
    /// </summary>
    public static bool IsActive => _isActive.Value;

    /// <summary>
    /// Marks the current async context as executing within a background job.
    /// Returns an IDisposable that clears the marker when disposed.
    /// </summary>
    /// <returns>IDisposable to clear the background job context</returns>
    public static IDisposable Use()
    {
        _isActive.Value = true;
        return new DisposeAction(() => _isActive.Value = false);
    }

    private sealed class DisposeAction : IDisposable
    {
        private readonly Action _action;
        private bool _disposed;

        public DisposeAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _action?.Invoke();
                _disposed = true;
            }
        }
    }
}

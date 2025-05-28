using Medallion.Threading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.GrantManager.Locks
{
    public class InMemoryDistributedSynchronizationHandle(SemaphoreSlim semaphore) : IDistributedSynchronizationHandle
    {
        private bool _disposed = false;
#pragma warning disable S3604 // Member initializer values should not be redundant
        private readonly CancellationTokenSource _cancellationTokenSource = new();
#pragma warning restore S3604 // Member initializer values should not be redundant

        public CancellationToken HandleLostToken => _cancellationTokenSource.Token;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    semaphore.Release();
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                semaphore.Release();
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource.Dispose();
                _disposed = true;
            }
            await ValueTask.CompletedTask;
            GC.SuppressFinalize(this);
        }
    }
}

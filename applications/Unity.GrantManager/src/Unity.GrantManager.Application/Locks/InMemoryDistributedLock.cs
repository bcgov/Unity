using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;

namespace Unity.GrantManager.Locks
{
    public class InMemoryDistributedLock(string name, ConcurrentDictionary<string, SemaphoreSlim> locks) : IDistributedLock
    {
        public string Name => name;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var semaphore = locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
            if (!semaphore.Wait(timeout ?? TimeSpan.FromSeconds(30), cancellationToken))
            {
                throw new TimeoutException("Failed to acquire lock within the specified timeout.");
            }
            return new InMemoryDistributedSynchronizationHandle(semaphore);
        }

        public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var semaphore = locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
            if (!await semaphore.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken))
            {
                throw new TimeoutException("Failed to acquire lock within the specified timeout.");
            }
            return new InMemoryDistributedSynchronizationHandle(semaphore);
        }

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            var semaphore = locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
            if (!semaphore.Wait(timeout, cancellationToken))
            {
                return null;
            }
            return new InMemoryDistributedSynchronizationHandle(semaphore);
        }

        public async ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default)
        {
            var semaphore = locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
            if (!await semaphore.WaitAsync(timeout, cancellationToken))
            {
                return null;
            }
            return new InMemoryDistributedSynchronizationHandle(semaphore);
        }
    }
}

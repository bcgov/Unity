using Medallion.Threading;
using System.Collections.Concurrent;
using System.Threading;

namespace Unity.GrantManager.Locks
{
    public class InMemoryDistributedLockProvider : IDistributedLockProvider
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public IDistributedLock CreateLock(string name)
        {
            return new InMemoryDistributedLock(name, _locks);
        }
    }
}

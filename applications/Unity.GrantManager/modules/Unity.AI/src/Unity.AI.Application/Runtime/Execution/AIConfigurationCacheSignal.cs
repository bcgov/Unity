using Microsoft.Extensions.Primitives;
using System.Threading;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime.Execution;

/// <summary>
/// Expires the cached AI operation settings on this server when models, operations or
/// prompts change, so Unity AI Setup edits apply without a restart. Other web pods still
/// keep their own entries until the cache duration runs out.
/// </summary>
public class AIConfigurationCacheSignal : ISingletonDependency
{
    private CancellationTokenSource _source = new();

    public IChangeToken GetChangeToken()
    {
        return new CancellationChangeToken(Volatile.Read(ref _source).Token);
    }

    public void Reset()
    {
        var previous = Interlocked.Exchange(ref _source, new CancellationTokenSource());
        // Not disposed: a caller may still be reading the old token. It has no timer, so GC is enough.
        previous.Cancel();
    }
}

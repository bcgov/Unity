using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

/// <summary>
/// Runs an async operation across a collection of items using a configurable execution mode.
/// Stateless and intentionally tiny: callers resolve mode + batch size and delegate iteration here.
/// </summary>
public static class AIExecutionStrategy
{
    public static async Task<List<TResult>> RunAsync<T, TResult>(
        IReadOnlyCollection<T> items,
        AIExecutionMode mode,
        int batchSize,
        Func<T, Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(operation);

        if (items.Count == 0)
        {
            return [];
        }

        switch (mode)
        {
            case AIExecutionMode.Parallel:
                return [.. await Task.WhenAll(items.Select(operation))];

            case AIExecutionMode.Batch:
                var batched = new List<TResult>(items.Count);
                foreach (var batch in items.Chunk(Math.Max(1, batchSize)))
                {
                    batched.AddRange(await Task.WhenAll(batch.Select(operation)));
                }
                return batched;

            default:
                var sequential = new List<TResult>(items.Count);
                foreach (var item in items)
                {
                    sequential.Add(await operation(item));
                }
                return sequential;
        }
    }
}

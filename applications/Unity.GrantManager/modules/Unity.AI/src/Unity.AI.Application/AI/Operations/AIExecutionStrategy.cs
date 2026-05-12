using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

/// <summary>
/// Runs an async operation across a collection of items using a configurable execution mode.
/// Stateless and intentionally tiny: callers resolve mode and provide item/batch work.
/// </summary>
public static class AIExecutionStrategy
{
    public static async Task<List<TResult>> RunAsync<T, TResult>(
        IReadOnlyCollection<T> items,
        AIExecutionMode mode,
        Func<T, Task<TResult>> operation,
        Func<IReadOnlyCollection<T>, Task<List<TResult>>> batchOperation)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(batchOperation);

        if (items.Count == 0)
        {
            return [];
        }

        switch (mode)
        {
            case AIExecutionMode.Parallel:
                return [.. await Task.WhenAll(items.Select(operation))];

            case AIExecutionMode.Batch:
                return await batchOperation(items);

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

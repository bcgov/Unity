using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.GrantManager.TestHelpers
{
    internal class TestAsyncEnumerableQueryable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerableQueryable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerableQueryable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return default;
        }
    }

    internal class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IQueryProvider, IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
            => new TestAsyncEnumerableQueryable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerableQueryable<TElement>(expression);

        public object? Execute(Expression expression)
            => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression)
            => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            // TResult is typically Task<T> for async operations
            var resultType = typeof(TResult);
            
            // Get the actual result synchronously
            var syncResult = inner.Execute(expression);
            
            // If TResult is Task<T>, extract T and wrap the result
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskResultType = resultType.GetGenericArguments()[0];
                var taskFromResult = typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(taskResultType);
                return (TResult)taskFromResult.Invoke(null, new[] { syncResult })!;
            }

            // For non-generic Task or other types, just return as-is
            return (TResult)(object)Task.CompletedTask;
        }
    }

    internal static class TestQueryableExtensions
    {
        public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
            => new TestAsyncEnumerableQueryable<T>(source);
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public class AIPromptCaptureStore : IAIPromptCaptureStore, ISingletonDependency
    {
        private const int MaxCapturesPerKey = 50;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<AIPromptCaptureResponse>> _captures = new(StringComparer.OrdinalIgnoreCase);

        public void Save(AIPromptCaptureResponse capture)
        {
            var key = BuildKey(capture.ContextId, capture.PromptType, capture.PromptVersion);
            var queue = _captures.GetOrAdd(key, _ => new ConcurrentQueue<AIPromptCaptureResponse>());
            queue.Enqueue(capture);

            while (queue.Count > MaxCapturesPerKey)
            {
                queue.TryDequeue(out _);
            }
        }

        public IReadOnlyList<AIPromptCaptureResponse> GetRecent(string contextId, string promptType, string? promptVersion = null, int maxResults = 20)
        {
            if (!string.IsNullOrWhiteSpace(promptVersion))
            {
                var key = BuildKey(contextId, promptType, promptVersion);
                return _captures.TryGetValue(key, out var captures)
                    ? captures.OrderByDescending(item => item.CapturedAt).Take(maxResults).ToList()
                    : Array.Empty<AIPromptCaptureResponse>();
            }

            return _captures.Values
                .SelectMany(queue => queue)
                .Where(item => string.Equals(item.ContextId, contextId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.PromptType, promptType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.CapturedAt)
                .Take(maxResults)
                .ToList();
        }

        private static string BuildKey(string contextId, string promptType, string promptVersion)
        {
            return $"{contextId.Trim()}::{promptType.Trim()}::{promptVersion.Trim()}";
        }
    }
}

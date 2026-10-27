
using System;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants
{
    public static class QueueingConstants
    {
        public const string DeadletterAddition = "-deadletter";
        public const string RetryAddition = ".retry";
        public const string RetryCountHeader = "x-retry-count";
        public const int MaxMessageRetries = 3;
        public const int MAX_RABBIT_CONCURRENT_CONSUMERS = 10;

        public static readonly TimeSpan[] RetryDelays =
        [
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(10)
        ];
    }
}

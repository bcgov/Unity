using System;
using Microsoft.Extensions.Configuration;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class RabbitMQOptions(IConfiguration configuration)
    {
        public const string SectionName = "RabbitMQ";

        public string UserName { get; set; } = configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest";
        public string Password { get; set; } = configuration.GetValue<string>("RabbitMQ:Password") ?? "guest";
        public string HostName { get; set; } = configuration.GetValue<string>("RabbitMQ:HostName") ?? "localhost";
        public string VirtualHost { get; set; } = configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/";
        public int Port { get; set; } = configuration.GetValue("RabbitMQ:Port", 5672);
        public int ConsumerDispatchConcurrency { get; set; } = QueueingConstants.MAX_RABBIT_CONCURRENT_CONSUMERS;
        public bool AutomaticRecoveryEnabled { get; set; } = true;
        public bool DispatchConsumersAsync { get; set; } = true;
        public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan RequestedConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan RequestedHeartbeat { get; set; } = TimeSpan.FromSeconds(60);
    }
}



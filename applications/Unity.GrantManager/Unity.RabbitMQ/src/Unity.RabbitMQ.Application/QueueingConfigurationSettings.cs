namespace Unity.RabbitMQ
{
    public class QueueingConfigurationSettings
    {
        public required string RabbitMqHostname { get; set; }
        public required string RabbitMqUsername { get; set; }
        public required string RabbitMqPassword { get; set; }
        public int? RabbitMqPort { get; set; }
        public int? RabbitMqConsumerConcurrency { get; set; }
    }
}

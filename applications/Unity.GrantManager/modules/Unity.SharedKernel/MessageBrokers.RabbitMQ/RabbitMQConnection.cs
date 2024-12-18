
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class RabbitMQConnection
    {
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;

        public RabbitMQConnection(IOptions<RabbitMQOptions> rabbitMQOptions)
        {
            _rabbitMQOptions = rabbitMQOptions;
        }
        public IConnection GetConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQOptions.Value.HostName,
                Port = _rabbitMQOptions.Value.Port,
                UserName = _rabbitMQOptions.Value.UserName,
                Password = _rabbitMQOptions.Value.Password,
                VirtualHost = _rabbitMQOptions.Value.VirtualHost
            };
            return factory.CreateConnection();
        }
    }
}
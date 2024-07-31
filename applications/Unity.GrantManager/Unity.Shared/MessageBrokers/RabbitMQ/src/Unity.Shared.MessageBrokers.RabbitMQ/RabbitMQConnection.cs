
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Unity.Shared.MessageBrokers.RabbitMQ
{
    public class RabbitMQConnection
    {
        private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;

        public RabbitMQConnection(IOptions<RabbitMQOptions> rabbitMQOptions) { 
            _rabbitMQOptions = rabbitMQOptions;
        }
        public IConnection GetConnection()
        {
            var factory = new ConnectionFactory();
            factory.HostName = _rabbitMQOptions.Value.HostName;
            factory.Port = _rabbitMQOptions.Value.Port;
            factory.UserName = _rabbitMQOptions.Value.UserName;
            factory.Password = _rabbitMQOptions.Value.Password;
            factory.VirtualHost = _rabbitMQOptions.Value.VirtualHost;
            return factory.CreateConnection();
        }
    }
}
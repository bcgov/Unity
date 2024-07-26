namespace Unity.Shared.MessageBrokers.RabbitMQ.Exceptions
{
    public class QueueingException : Exception
    {
        public QueueingException(string message, Exception ex) : base(message, ex)
        {
        }

        public QueueingException(string message) : base(message)
        {
        }
    }
}

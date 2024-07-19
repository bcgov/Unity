namespace Unity.RabbitMQ.Interfaces
{ 
    public interface IQueueMessage
    {
        Guid MessageId { get; set; }
        TimeSpan TimeToLive { get; set; }
    }
}

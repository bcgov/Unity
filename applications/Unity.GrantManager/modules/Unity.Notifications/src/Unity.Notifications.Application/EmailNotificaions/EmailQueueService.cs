using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Text;
using Unity.Notifications.Integrations.RabbitMQ;
using System;
using Microsoft.Extensions.Options;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Events;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.EmailNotificaions;

public class EmailQueueService : ApplicationService
{
    private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
    public const string UNITY_EMAIL_QUEUE = "unity_emails";

    public EmailQueueService(IOptions<RabbitMQOptions> rabbitMQOptions) {
        _rabbitMQOptions = rabbitMQOptions;
    }

    private static int FiveMinutesInMilliSeconds = 300000;

    public async Task<Task> SendToEmailDelayedQueueAsync(EmailNotificationEvent emailNotificationEvent)
    {
        RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
        IConnection connection = rabbitMQConnection.GetConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: UNITY_EMAIL_QUEUE,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(emailNotificationEvent);
        var bodyPublish = Encoding.UTF8.GetBytes(json);

        IBasicProperties props = channel.CreateBasicProperties();
        await Task.Delay(TimeSpan.FromMilliseconds(FiveMinutesInMilliSeconds * (emailNotificationEvent.RetryAttempts+1)));

        channel.BasicPublish(exchange: string.Empty,
                            routingKey:UNITY_EMAIL_QUEUE,
                            basicProperties: props,
                            body: bodyPublish);

        return Task.CompletedTask;
    }

    public Task SendToEmailEventQueueAsync(EmailNotificationEvent emailNotificationEvent)
    {
        RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
        IConnection connection = rabbitMQConnection.GetConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: UNITY_EMAIL_QUEUE,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(emailNotificationEvent);
        var bodyPublish = Encoding.UTF8.GetBytes(json);
        IBasicProperties props = channel.CreateBasicProperties();
        channel.BasicPublish(exchange: string.Empty,
                            routingKey: UNITY_EMAIL_QUEUE,
                            basicProperties: props,
                            body: bodyPublish);

        return Task.CompletedTask;
    }
}
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using RabbitMQ.Client;
using System.Text;
using Unity.Notifications.Emails;
using Unity.Notifications.Integrations.RabbitMQ;
using System;
using Microsoft.Extensions.Options;
using Unity.Notifications.EmailNotifications;

namespace Unity.Notifications.EmailNotificaions;

public class EmailQueueService : ApplicationService
{

    private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
    public static string UNITY_EMAIL_QUEUE = "unity_emails";

    public EmailQueueService(IOptions<RabbitMQOptions> rabbitMQOptions) {
        _rabbitMQOptions = rabbitMQOptions;
    }

    private static int FiveMinutesInMilliSeconds = 300000;

    public async Task<Task> SendToEmailDelayedQueueAsync(EmailLog emailLog)
    {
        RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
        IConnection connection = rabbitMQConnection.GetConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: UNITY_EMAIL_QUEUE,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(emailLog);
        var bodyPublish = Encoding.UTF8.GetBytes(json);

        IBasicProperties props = channel.CreateBasicProperties();
        await Task.Delay(TimeSpan.FromMilliseconds(FiveMinutesInMilliSeconds * (emailLog.RetryAttempts+1)));

        channel.BasicPublish(exchange: string.Empty,
                            routingKey:UNITY_EMAIL_QUEUE,
                            basicProperties: props,
                            body: bodyPublish);

        return Task.CompletedTask;
    }

    public Task SendToEmailQueueAsync(EmailLog emailLog)
    {
        RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
        IConnection connection = rabbitMQConnection.GetConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: UNITY_EMAIL_QUEUE,
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(emailLog);
        var bodyPublish = Encoding.UTF8.GetBytes(json);
        IBasicProperties props = channel.CreateBasicProperties();
        channel.BasicPublish(exchange: string.Empty,
                            routingKey: UNITY_EMAIL_QUEUE,
                            basicProperties: props,
                            body: bodyPublish);

        return Task.CompletedTask;
    }
}
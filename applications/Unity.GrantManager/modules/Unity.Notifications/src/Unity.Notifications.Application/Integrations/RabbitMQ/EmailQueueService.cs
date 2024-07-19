using System.Threading.Tasks;
using System;
using Unity.Notifications.Events;
using Volo.Abp.Application.Services;
using Unity.RabbitMQ.Interfaces;
using Unity.Notifications.Integrations.RabbitMQ.QueueMessages;

namespace Unity.Notifications.Integrations.RabbitMQ;

public class EmailQueueService : ApplicationService
{
    private readonly IQueueProducer<EmailMessages> _queueProducer;
    private static int FiveMinutesInMilliSeconds = 300000;

    public EmailQueueService(IQueueProducer<EmailMessages> queueProducer)
    {
        _queueProducer = queueProducer;
    }

    public async Task<Task> SendToEmailDelayedQueueAsync(EmailNotificationEvent emailNotificationEvent)
    {
        var message = new EmailMessages
        {
            TimeToLive = TimeSpan.FromMinutes(1),
            EmailNotificationEvent = emailNotificationEvent
        };

        await Task.Delay(TimeSpan.FromMilliseconds(FiveMinutesInMilliSeconds * (emailNotificationEvent.RetryAttempts + 1)));

        _queueProducer.PublishMessage(message);
        return Task.CompletedTask;
    }

    public Task SendToEmailEventQueueAsync(EmailNotificationEvent emailNotificationEvent)
    {
        var message = new EmailMessages
        {
            TimeToLive = TimeSpan.FromMinutes(1),
            EmailNotificationEvent = emailNotificationEvent
        };
        _queueProducer.PublishMessage(message);
        return Task.CompletedTask;
    }
}
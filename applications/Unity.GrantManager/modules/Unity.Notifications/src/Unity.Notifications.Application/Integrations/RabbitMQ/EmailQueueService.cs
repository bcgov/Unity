using System.Threading.Tasks;
using System;
using Unity.Notifications.Events;
using Volo.Abp.Application.Services;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Notifications.Integrations.RabbitMQ.QueueMessages;
using Microsoft.Extensions.Logging;

namespace Unity.Notifications.Integrations.RabbitMQ;

public class EmailQueueService : ApplicationService
{
    private readonly IQueueProducer<EmailMessages> _queueProducer;
    private readonly ILogger<EmailQueueService> _logger;
    private const int FiveMinutesInMilliSeconds = 300000;
    private const int TenMintues = 10;
    private const int TwentyMintues = 20;

    public EmailQueueService(IQueueProducer<EmailMessages> queueProducer,
         ILogger<EmailQueueService> logger)
    {
        _queueProducer = queueProducer;
        _logger = logger;
    }

    public async Task<Task> SendToEmailDelayedQueueAsync(EmailNotificationEvent emailNotificationEvent)
    {
        try
        {
            var message = new EmailMessages
            {
                TimeToLive = TimeSpan.FromMinutes(TwentyMintues),
                EmailNotificationEvent = emailNotificationEvent
            };

            await Task.Delay(TimeSpan.FromMilliseconds(FiveMinutesInMilliSeconds * (emailNotificationEvent.RetryAttempts + 1)));

            _queueProducer.PublishMessage(message);
        }
        catch (Exception ex) {
            var ExceptionMessage = ex.Message;
            _logger.LogError(ex, "SendToEmailDelayedQueueAsync Exception: {ExceptionMessage}", ExceptionMessage);
        }
        return Task.CompletedTask;
    }

    public Task SendToEmailEventQueueAsync(EmailNotificationEvent emailNotificationEvent)
    {
        try
        {
            var message = new EmailMessages
            {
                TimeToLive = TimeSpan.FromMinutes(TenMintues),
                EmailNotificationEvent = emailNotificationEvent
            };
            _queueProducer.PublishMessage(message);
        }
        catch (Exception ex) {
            var ExceptionMessage = ex.Message;
            _logger.LogError(ex, "SendToEmailEventQueueAsync Exception: {ExceptionMessage}", ExceptionMessage);
        }

        return Task.CompletedTask;
    }
}
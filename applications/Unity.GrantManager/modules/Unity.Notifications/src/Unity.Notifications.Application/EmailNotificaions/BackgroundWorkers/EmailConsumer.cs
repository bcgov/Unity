using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using Unity.Notifications.Emails;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Unity.Notifications.Integrations.RabbitMQ;
using System.Text;
using Unity.Notifications.EmailNotificaions;
using System;
using Volo.Abp.Uow;
using RestSharp;
using System.Net;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Unity.Notifications.Events;
using Volo.Abp.MultiTenancy;
using Volo.Abp;

namespace Unity.Notifications.EmailNotifications;

public class EmailConsumer : QuartzBackgroundWorkerBase
{
    private readonly EmailQueueService _emailQueueService;
    private int _retryAttemptMax {get; set; } = 0;
    private readonly IEmailLogsRepository _emailLogsRepository;
    private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ICurrentTenant _currentTenant;
#pragma warning restore S4487 // Unread "private" fields should be removed

    public EmailConsumer(
        IEmailLogsRepository emailLogsRepository,
        IOptions<EmailBackgroundJobsOptions> emailBackgroundJobsOptions,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        EmailQueueService  emailQueueService,
        IUnitOfWorkManager unitOfWorkManager,
        ICurrentTenant currentTenant
        )
    {
        _rabbitMQOptions = rabbitMQOptions;
        _emailQueueService = emailQueueService;
        JobDetail = JobBuilder.Create<EmailConsumer>().WithIdentity(nameof(EmailConsumer)).Build();
        _retryAttemptMax = emailBackgroundJobsOptions.Value.EmailResend.RetryAttemptsMaximum;
        _emailLogsRepository = emailLogsRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _currentTenant = currentTenant;

        Trigger = TriggerBuilder.Create().WithIdentity(nameof(EmailConsumer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(emailBackgroundJobsOptions.Value.EmailResend.Expression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    private static bool ReprocessBasedOnStatusCode(HttpStatusCode statusCode)
    {
        HttpStatusCode[] reprocessStatusCodes = {
             HttpStatusCode.TooManyRequests,
             HttpStatusCode.InternalServerError,
             HttpStatusCode.BadGateway,
             HttpStatusCode.ServiceUnavailable,
             HttpStatusCode.GatewayTimeout,
        };

        return reprocessStatusCodes.Contains(statusCode);
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        ProcessEmailMessages();
        await Task.CompletedTask;
    }

    public void ProcessEmailMessages()
    {
        RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
        IConnection connection = rabbitMQConnection.GetConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(queue: EmailQueueService.UNITY_EMAIL_QUEUE,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            EmailNotificationEvent? emailNotificationEvent = JsonConvert.DeserializeObject<EmailNotificationEvent>(message);

            if(emailNotificationEvent == null || emailNotificationEvent.TenantId == Guid.Empty || emailNotificationEvent.Id == Guid.Empty) {
                throw new UserFriendlyException("Notification Event null or no Tenant ID or Email Id");
            }

            // Grab the tenant and switch db context
            var uow = _unitOfWorkManager.Begin(true, false);
            using (_currentTenant.Change(emailNotificationEvent.TenantId))
            {
                EmailLog? emailLog = await _emailLogsRepository.GetAsync(emailNotificationEvent.Id);
                if (emailLog != null && emailLog.Id != Guid.Empty && emailLog.ToAddress != null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    try
                    {
                        // Resend the email - Update the RetryCount
                        if (emailLog.RetryAttempts <= _retryAttemptMax)
                        {
                            IEmailNotificationService _emailNotificationService = ServiceProvider.GetRequiredService<IEmailNotificationService>();
                            RestResponse response = await _emailNotificationService.SendEmailNotification(
                                                                                            emailLog.ToAddress, 
                                                                                            emailLog.Body, 
                                                                                            emailLog.Subject, 
                                                                                            emailLog.ApplicationId);
                            if(ReprocessBasedOnStatusCode(response.StatusCode))
                            {
                                emailLog.RetryAttempts = emailLog.RetryAttempts + 1;
                                EmailLog updatedEmailLog = await _emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                                await uow.SaveChangesAsync();
                                emailNotificationEvent.RetryAttempts = emailLog.RetryAttempts;
                                await _emailQueueService.SendToEmailDelayedQueueAsync(emailNotificationEvent);
                            }
                        }
                    } catch (Exception ex)
                    {
                        string ExceptionMessage = ex.Message;
                        Logger.LogInformation(ex, "Process Delayed Email Exception: {ExceptionMessage}", ExceptionMessage);
                    }
                }

            }
        };

        channel.BasicConsume(queue: EmailQueueService.UNITY_EMAIL_QUEUE, autoAck: false, consumer: consumer);
    }

}



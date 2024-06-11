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

namespace Unity.Notifications.EmailNotifications;

public class EmailConsumer : QuartzBackgroundWorkerBase
{
    private readonly IOptions<EmailBackgroundJobsOptions> _emailBackgroundJobsOptions;
    private EmailQueueService _emailQueueService;

    private int _retryAttemptMax = 0;
    private readonly IEmailNotificationService _emailNotificationService;
    private IEmailLogsRepository _emailLogsRepository;
    private readonly IOptions<RabbitMQOptions> _rabbitMQOptions;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EmailConsumer(
        IEmailNotificationService emailNotificationService,
        IEmailLogsRepository emailLogsRepository,
        IOptions<EmailBackgroundJobsOptions> emailBackgroundJobsOptions,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        EmailQueueService  emailQueueService,
        IUnitOfWorkManager unitOfWorkManager
        )
    {
        _emailNotificationService = emailNotificationService;
        _emailBackgroundJobsOptions = emailBackgroundJobsOptions;
        _rabbitMQOptions = rabbitMQOptions;
        _emailQueueService = emailQueueService;
        JobDetail = JobBuilder.Create<EmailConsumer>().WithIdentity(nameof(EmailConsumer)).Build();
        _retryAttemptMax = _emailBackgroundJobsOptions.Value.EmailResend.RetryAttemptsMaximum;
        _emailLogsRepository = emailLogsRepository;
        _unitOfWorkManager = unitOfWorkManager;

        Trigger = TriggerBuilder.Create().WithIdentity(nameof(EmailConsumer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(_emailBackgroundJobsOptions.Value.EmailResend.Expression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    private bool ReprocessBasedOnStatusCode(HttpStatusCode statusCode)
    {
        HttpStatusCode[] reprocessStatusCodes = new HttpStatusCode[] {
             HttpStatusCode.Unauthorized,
             HttpStatusCode.Forbidden,
             HttpStatusCode.NotFound,
             HttpStatusCode.Conflict,
             HttpStatusCode.Locked,
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
        Logger.LogInformation("Executing EmailConsumer...");
        ProcessDelayedEmailMessages();
        await Task.CompletedTask;
    }

    public void ProcessDelayedEmailMessages()
    {
        RabbitMQConnection rabbitMQConnection = new RabbitMQConnection(_rabbitMQOptions);
        IConnection connection = rabbitMQConnection.GetConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "unity_emails",
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray(); ;
            var message = Encoding.UTF8.GetString(body);
            EmailLog? emailLog = JsonConvert.DeserializeObject<EmailLog>(message);
            using (var uow = _unitOfWorkManager.Begin(true, false))
            {
                if (emailLog != null && emailLog.Id != Guid.Empty && emailLog.ToAddress != null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    try
                    {
                        // Resend the email - Update the RetryCount
                        if (emailLog.RetryAttempts <= _retryAttemptMax)
                        {
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
                                await _emailQueueService.SendToEmailDelayedQueueAsync(updatedEmailLog);
                            }
                        }
                    } catch (Exception ex)
                    {
                        Logger.LogInformation(ex.Message);
                    }
                }

            }

        };

        channel.BasicConsume(queue: "unity_emails", autoAck: false, consumer: consumer);
    }

}



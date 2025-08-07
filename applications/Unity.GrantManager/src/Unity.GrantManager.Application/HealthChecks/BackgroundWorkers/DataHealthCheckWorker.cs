using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Settings;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.EmailNotifications;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Enums;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.HealthChecks.BackgroundWorkers
{
    [DisallowConcurrentExecution]
    public class DataHealthCheckWorker : QuartzBackgroundWorkerBase
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ITenantRepository _tenantRepository;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IPaymentRequestRepository _paymentRequestsRepository;

        public DataHealthCheckWorker(ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            ISettingManager settingManager,
            IEmailNotificationService emailNotificationService,
            IPaymentRequestRepository paymentRequestsRepository)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _emailNotificationService = emailNotificationService;
            _paymentRequestsRepository = paymentRequestsRepository;


            string cronExpression = SettingDefinitions.GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.DataHealthCheckMonitor_Expression);

            JobDetail = JobBuilder
                .Create<DataHealthCheckWorker>()
                .WithIdentity(nameof(DataHealthCheckWorker))
                .Build();

            Trigger = TriggerBuilder
                .Create()
                .WithIdentity(nameof(DataHealthCheckWorker))
                .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
                .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            Logger.LogInformation("Executing DataHealthCheckWorker...");


            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.Equals(envInfo, "Production", StringComparison.OrdinalIgnoreCase))
            {

                var tenants = await _tenantRepository.GetListAsync();
                bool sendEmail = false;
                var emailBodyBuilder = new System.Text.StringBuilder();

                foreach (var tenant in tenants)
                {
                    using (_currentTenant.Change(tenant.Id, tenant.Name))
                    {
                        // Lookup the missing emails
                        var missingEmailsCount = await _emailNotificationService.GetEmailsChesWithNoResponseCountAsync();
                        if (missingEmailsCount > 0)
                        {
                            Logger.LogWarning("Tenant {TenantName} has {MissingEmailsCount} missing email(s) with a status of Initialized or Sent but no CHES Response.", tenant.Name, missingEmailsCount);
                            string missingEmailBody = $"Unity tenant {tenant.Name} has {missingEmailsCount} email(s) that were sent but have no CHES Response.";
                            sendEmail = true;
                            emailBodyBuilder.AppendLine($"{missingEmailBody}<br />");
                        }
                        // Lookup the missing payments
                        var missingPayments = await GetPaymentsSentWithoutResponseCountAsync();
                        if (missingPayments > 0)
                        {
                            Logger.LogWarning("Tenant {TenantName} has {MissingPaymentsCount} payments sent without a response.", tenant.Name, missingPayments);
                            string missingPaymentBody = $"Unity tenant {tenant.Name} has {missingPayments} payment(s) that are in Submitted status but have no CAS Response.";
                            sendEmail = true;
                            emailBodyBuilder.AppendLine($"{missingPaymentBody}<br />");
                        }
                    }
                }

                if (sendEmail)
                {
                    string emailBody = emailBodyBuilder.ToString();
                    await SendEmailAlert(emailBody, "Data Health Check Alert - Emails/Payments Missing Responses");
                }

                Logger.LogInformation("DataHealthCheckWorker Executed...");
                await Task.CompletedTask;
            }
        }

        private async Task<int> GetPaymentsSentWithoutResponseCountAsync()
        {
            var payments = await _paymentRequestsRepository.GetListAsync(x => x.Status == PaymentRequestStatus.Submitted && x.CasHttpStatusCode == null);
            return payments.Count;
        }

        private async Task SendEmailAlert(string emailBody, string subject)
        {

            string htmlBody = $@"
            <html>
                <body style='font-family: Arial, sans-serif;'>
                    <p>{emailBody}</p>
                    <br />
                    <p style='font-size: 12px; color: #999;'>*Note - Please do not reply to this email as it is an automated notification.</p>
                </body>
            </html>";

            await _emailNotificationService.SendEmailNotification(
                "grantmanagementsupport@gov.bc.ca",
                htmlBody,
                subject,
                "NoReply@gov.bc.ca", "html",
                "");

            Logger.LogInformation("Missing Alerts Sent...");

        }
    }
}

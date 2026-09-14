using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Features;
using Volo.Abp.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Notifications;

/// <summary>
/// Recovers the commit-to-queue gap. Delivery failures are retried by the existing EmailConsumer.
/// </summary>
[DisallowConcurrentExecution]
public class ApplicantUpdateEmailRecoveryWorker : QuartzBackgroundWorkerBase
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicantUpdateEmailRecoveryWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        JobDetail = JobBuilder.Create<ApplicantUpdateEmailRecoveryWorker>()
            .WithIdentity(nameof(ApplicantUpdateEmailRecoveryWorker)).Build();
        Trigger = TriggerBuilder.Create().WithIdentity(nameof(ApplicantUpdateEmailRecoveryWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule("0 0/5 * * * ?")
                .WithMisfireHandlingInstructionIgnoreMisfires()).Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var currentTenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        List<Tenant> tenants;
        using (currentTenant.Change(null))
        using (var uow = uowManager.Begin(requiresNew: true))
        {
            tenants = await scope.ServiceProvider.GetRequiredService<ITenantRepository>().GetListAsync();
            await uow.CompleteAsync();
        }

        foreach (var tenant in tenants)
        {
            try
            {
                using var tenantScope = _serviceProvider.CreateScope();
                using (tenantScope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenant.Id))
                {
                    await RecoverTenantAsync(tenantScope.ServiceProvider);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not recover pending applicant update emails for tenant {TenantId}.", tenant.Id);
            }
        }
    }

    private static async Task RecoverTenantAsync(IServiceProvider services)
    {
        var uowManager = services.GetRequiredService<IUnitOfWorkManager>();
        List<EmailLog> pending;
        using (var uow = uowManager.Begin(requiresNew: true))
        {
            if (!await services.GetRequiredService<IFeatureChecker>().IsEnabledAsync("Unity.Notifications"))
            {
                return;
            }
            var repository = services.GetRequiredService<IEmailLogsRepository>();
            var cutoff = services.GetRequiredService<IClock>().Now.AddMinutes(-10);
            var query = RecoverableEmails(await repository.GetQueryableAsync(), cutoff);
            pending = await services.GetRequiredService<IAsyncQueryableExecuter>().ToListAsync(query);
            await uow.CompleteAsync();
        }

        var manager = services.GetRequiredService<IEmailNotificationManager>();
        foreach (var email in pending)
        {
            try
            {
                using var queueUow = uowManager.Begin(requiresNew: true, isTransactional: false);
                await manager.QueueEmailAsync(email);
                await queueUow.CompleteAsync();
            }
            catch (Exception ex)
            {
                services.GetRequiredService<ILogger<ApplicantUpdateEmailRecoveryWorker>>()
                    .LogError(ex, "Could not requeue applicant update email {EmailId} for tenant {TenantId}.", email.Id, email.TenantId);
            }
        }
    }

    public static IQueryable<EmailLog> RecoverableEmails(IQueryable<EmailLog> emails, DateTime cutoff) =>
        emails.Where(email => email.Tag == ApplicantUpdateNotificationService.EmailTag
                && email.Status == EmailStatus.Initialized && email.RetryAttempts == 0
                && email.CreationTime < cutoff)
            .OrderBy(email => email.CreationTime)
            .Take(100);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Quartz;
using Shouldly;
using Unity.GrantManager.GrantsPortal.Notifications;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Volo.Abp.Features;
using Volo.Abp.Linq;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class ApplicantUpdateEmailRecoveryWorkerTests
{
    [Fact]
    public async Task ShouldRequeueOnlyStaleUnattemptedPortalEmailsWithOriginalIdsAndContent()
    {
        var now = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var tenantId = Guid.NewGuid();
        var pending = Email(now.AddMinutes(-11));
        pending.TenantId = tenantId;
        var originalBody = pending.Body;
        var sent = Email(now.AddHours(-1));
        sent.Status = EmailStatus.Sent;
        var failed = Email(now.AddHours(-1));
        failed.Status = EmailStatus.Failed;
        var cancelled = Email(now.AddHours(-1));
        cancelled.Status = EmailStatus.Cancelled;
        var attempted = Email(now.AddHours(-1));
        attempted.RetryAttempts = 1;
        var unrelated = Email(now.AddHours(-1));
        unrelated.Tag = "Other notification";
        var recent = Email(now.AddMinutes(-9));
        var boundary = Email(now.AddMinutes(-10));
        var records = new[] { pending, sent, failed, cancelled, attempted, unrelated, recent, boundary };

        var tenants = Substitute.For<ITenantRepository>();
        tenants.GetListAsync().Returns(new List<Tenant> { CreateTenant(tenantId) });
        var logs = Substitute.For<IEmailLogsRepository>();
        logs.GetQueryableAsync().Returns(records.AsQueryable());
        var executer = Substitute.For<IAsyncQueryableExecuter>();
        executer.ToListAsync(Arg.Any<IQueryable<EmailLog>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<IQueryable<EmailLog>>(0).ToList());
        var clock = Substitute.For<IClock>();
        clock.Now.Returns(now);
        var features = Substitute.For<IFeatureChecker>();
        features.IsEnabledAsync("Unity.Notifications").Returns(true);
        var currentTenant = Substitute.For<ICurrentTenant>();
        var manager = Substitute.For<IEmailNotificationManager>();
        var uowManager = Substitute.For<IUnitOfWorkManager>();
        uowManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>()).Returns(_ => Substitute.For<IUnitOfWork>());

        var services = new ServiceCollection();
        services.AddSingleton(tenants);
        services.AddSingleton(logs);
        services.AddSingleton(executer);
        services.AddSingleton(clock);
        services.AddSingleton(features);
        services.AddSingleton(currentTenant);
        services.AddSingleton(manager);
        services.AddSingleton(uowManager);
        using var provider = services.BuildServiceProvider();
        var worker = new ApplicantUpdateEmailRecoveryWorker(provider);

        await worker.Execute(Substitute.For<IJobExecutionContext>());

        await manager.Received(1).QueueEmailAsync(Arg.Any<EmailLog>());
        await manager.Received(1).QueueEmailAsync(pending);
        currentTenant.Received().Change(tenantId);
        pending.Body.ShouldBe(originalBody);
        pending.RetryAttempts.ShouldBe(0);
    }

    [Fact]
    public void ShouldBoundRecoveryBatchesAndProcessOldestFirst()
    {
        var cutoff = new DateTime(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var emails = Enumerable.Range(1, 105).Select(minutes => Email(cutoff.AddMinutes(-minutes))).ToArray();

        var recovered = ApplicantUpdateEmailRecoveryWorker.RecoverableEmails(emails.AsQueryable(), cutoff).ToList();

        recovered.Count.ShouldBe(100);
        recovered[0].Id.ShouldBe(emails[104].Id);
        recovered[^1].Id.ShouldBe(emails[5].Id);
    }

    private static EmailLog Email(DateTime created) => new()
    {
        Id = Guid.NewGuid(),
        CreationTime = created,
        Tag = ApplicantUpdateNotificationService.EmailTag,
        Status = EmailStatus.Initialized,
        Body = "Original update snapshot"
    };

    // Match the non-public ABP constructor used by the other tenant unit-test fixtures.
    private static Tenant CreateTenant(Guid id)
    {
        var constructor = typeof(Tenant).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(Guid), typeof(string), typeof(string)], null)
            ?? throw new InvalidOperationException("Expected ABP Tenant constructor was not found.");
        return (Tenant)constructor.Invoke([id, "Tenant", "TENANT"]);
    }
}

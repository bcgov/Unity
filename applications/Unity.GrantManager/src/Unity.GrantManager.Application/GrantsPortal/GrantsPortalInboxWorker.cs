using System;
using Microsoft.Extensions.Options;
using Quartz;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.Messaging;

namespace Unity.GrantManager.GrantsPortal;

/// <summary>
/// Polls the central inbox table for pending GrantsPortal inbound messages and processes them sequentially.
/// All orchestration logic (retry, tenant switching, outbox ack) is handled by <see cref="InboxWorkerBase"/>.
/// Handlers are resolved as <see cref="IInboxMessageHandler"/> instances with Source == "GrantsPortal".
/// </summary>
public class GrantsPortalInboxWorker : InboxWorkerBase
{
    protected override string SourceName => GrantsPortalRabbitMqOptions.SourceName;

    public GrantsPortalInboxWorker(
        IServiceProvider serviceProvider,
        IOptions<GrantsPortalRabbitMqOptions> options)
        : base(serviceProvider)
    {
        var cronExpression = options.Value.InboxProcessorCron;

        JobDetail = JobBuilder
            .Create<GrantsPortalInboxWorker>()
            .WithIdentity(nameof(GrantsPortalInboxWorker))
            .Build();

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(GrantsPortalInboxWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(cronExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }
}

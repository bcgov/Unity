using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.SettingManagement;
using Unity.Modules.Shared.Utils;
using Unity.Payments.Settings;
using System;

namespace Unity.Payments.PaymentRequests;

[DisallowConcurrentExecution]
public class ReconciliationProducer : QuartzBackgroundWorkerBase
{
    private readonly CasPaymentRequestCoordinator _casPaymentRequestCoordinator;
    
    public ReconciliationProducer(
        ISettingManager settingManager,
        CasPaymentRequestCoordinator casPaymentRequestCoordinator
        )
    {
        _casPaymentRequestCoordinator = casPaymentRequestCoordinator;
        string casPaymentsProducerExpression = "";
        try { 
            casPaymentsProducerExpression = SettingDefinitions.GetSettingsValue(settingManager, PaymentSettingsConstants.BackgroundJobs.CasPaymentsReconciliation_ProducerExpression);
        } catch
        {
            casPaymentsProducerExpression = "0 0 9 1/1 * ? *";
        }

        if(!casPaymentsProducerExpression.IsNullOrEmpty()) {
            JobDetail = JobBuilder
                .Create<ReconciliationProducer>()
                .WithIdentity(nameof(ReconciliationProducer))
                .Build();

            Trigger = TriggerBuilder
                .Create()
                .WithIdentity(nameof(ReconciliationProducer))
                .WithSchedule(CronScheduleBuilder.CronSchedule(casPaymentsProducerExpression)
                .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();
        }
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await _casPaymentRequestCoordinator.AddPaymentRequestsToReconciliationQueue();
        await Task.CompletedTask;
    }    
}
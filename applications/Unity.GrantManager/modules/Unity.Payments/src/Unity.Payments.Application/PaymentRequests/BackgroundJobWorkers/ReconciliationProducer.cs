using System.Threading.Tasks;
using Quartz;
using Volo.Abp.BackgroundWorkers.Quartz;
using Unity.GrantManager.Settings;
using Volo.Abp.SettingManagement;
using Unity.Modules.Shared.Utils;

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
        JobDetail = JobBuilder
            .Create<ReconciliationProducer>()
            .WithIdentity(nameof(ReconciliationProducer))
            .Build();
        
        _casPaymentRequestCoordinator = casPaymentRequestCoordinator;
        string casPaymentsProducerExpression = SettingDefinitions.GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.CasPaymentsReconciliation_ProducerExpression);

        Trigger = TriggerBuilder
            .Create()
            .WithIdentity(nameof(ReconciliationProducer))
            .WithSchedule(CronScheduleBuilder.CronSchedule(casPaymentsProducerExpression)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
    }

    public override async Task Execute(IJobExecutionContext context)
    {
        await _casPaymentRequestCoordinator.AddPaymentRequestsToReconciliationQueue();
        await Task.CompletedTask;
    }    
}
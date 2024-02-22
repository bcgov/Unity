using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Intakes.BackgroundWorkers
{
    public class IntakeSyncWorker : QuartzBackgroundWorkerBase
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ITenantRepository _tenantRepository;
        private readonly IApplicationFormSycnronizationService _applicationFormSynchronizationService;

        public IntakeSyncWorker(ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            IApplicationFormSycnronizationService applicationFormSynchronizationService)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _applicationFormSynchronizationService = applicationFormSynchronizationService;

            JobDetail = JobBuilder.Create<IntakeSyncWorker>().WithIdentity(nameof(IntakeSyncWorker)).Build();

            Trigger = TriggerBuilder.Create().WithIdentity(nameof(IntakeSyncWorker))
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(01,00)
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            var tenants = await _tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id))
                {
                    _applicationFormSynchronizationService.GetMissingSubmissions();
                }
            }

            Logger.LogInformation("Executed IntakeSyncWorker..!");
            await Task.CompletedTask;
        }
    }
}

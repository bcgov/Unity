using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IOptions<BackgroundJobsOptions> _backgroundJobsOptions;

        public IntakeSyncWorker(ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            IApplicationFormSycnronizationService applicationFormSynchronizationService,
            IOptions<BackgroundJobsOptions> backgroundJobsOptions)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _applicationFormSynchronizationService = applicationFormSynchronizationService;
            _backgroundJobsOptions = backgroundJobsOptions;

            JobDetail = JobBuilder.Create<IntakeSyncWorker>().WithIdentity(nameof(IntakeSyncWorker)).Build();

            Trigger = TriggerBuilder.Create().WithIdentity(nameof(IntakeSyncWorker))
            .WithSchedule(CronScheduleBuilder.CronSchedule(_backgroundJobsOptions.Value.IntakeResync.Expression)                     
            .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            Logger.LogInformation("Executing IntakeSyncWorker...");

            var tenants = await _tenantRepository.GetListAsync();

            if(!int.TryParse(_backgroundJobsOptions.Value.IntakeResync.NumDaysToCheck, out int numberDaysBack)) {
                Logger.LogInformation("IntakeSyncWorker - Could not parse number of Days...");
                return;
            }

            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id))
                {
                    await _applicationFormSynchronizationService.GetMissingSubmissions(numberDaysBack);
                }
            }

            Logger.LogInformation("IntakeSyncWorker Executed...");
            await Task.CompletedTask;
        }
    }
}

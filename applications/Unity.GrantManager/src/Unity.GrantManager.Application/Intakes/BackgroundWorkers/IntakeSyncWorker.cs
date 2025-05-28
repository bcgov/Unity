using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Settings;
using Unity.Modules.Shared.Utils;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Intakes.BackgroundWorkers
{
    [DisallowConcurrentExecution]
    public class IntakeSyncWorker : QuartzBackgroundWorkerBase
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ITenantRepository _tenantRepository;
        private readonly IApplicationFormSycnronizationService _applicationFormSynchronizationService;
        private readonly string _numberOfDaysToCheck;

        public IntakeSyncWorker(ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            ISettingManager settingManager,
            IApplicationFormSycnronizationService applicationFormSynchronizationService)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _applicationFormSynchronizationService = applicationFormSynchronizationService;

            string intakeResyncExpression = SettingDefinitions.GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.IntakeResync_Expression);
            _numberOfDaysToCheck = SettingDefinitions.GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.IntakeResync_NumDaysToCheck);

            JobDetail = JobBuilder
                .Create<IntakeSyncWorker>()
                .WithIdentity(nameof(IntakeSyncWorker))
                .Build();

            Trigger = TriggerBuilder
                .Create()
                .WithIdentity(nameof(IntakeSyncWorker))
                .WithSchedule(CronScheduleBuilder.CronSchedule(intakeResyncExpression)
                .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            Logger.LogInformation("Executing IntakeSyncWorker...");
            var tenants = await _tenantRepository.GetListAsync();

            if (!int.TryParse(_numberOfDaysToCheck, out int numberDaysBack))
            {
                Logger.LogInformation("IntakeSyncWorker - Could not parse number of Days...");
                return;
            }

            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id, tenant.Name))
                {
                    await _applicationFormSynchronizationService.GetMissingSubmissions(numberDaysBack);
                }
            }

            Logger.LogInformation("IntakeSyncWorker Executed...");
            await Task.CompletedTask;
        }
    }
}

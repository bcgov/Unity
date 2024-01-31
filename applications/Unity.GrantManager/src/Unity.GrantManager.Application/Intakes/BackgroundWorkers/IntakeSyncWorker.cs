using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp.Application.Dtos;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Intakes.BackgroundWorkers
{
    public class IntakeSyncWorker : QuartzBackgroundWorkerBase
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ITenantRepository _tenantRepository;

        private readonly IApplicationFormAppService _appFormsService;

        public IntakeSyncWorker(ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            IApplicationFormAppService appFormsSerrvice)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _appFormsService = appFormsSerrvice;

            JobDetail = JobBuilder.Create<IntakeSyncWorker>().WithIdentity(nameof(IntakeSyncWorker)).Build();
            Trigger = TriggerBuilder.Create().WithIdentity(nameof(IntakeSyncWorker))
                .WithSimpleSchedule(s => s.WithIntervalInSeconds(10)
                .RepeatForever()
                .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();

            // -- Nightly job execution --
            //Trigger = TriggerBuilder.Create().WithIdentity(nameof(IntakeSyncWorker))
            //    .WithCronSchedule("0 0 0 * * *") -- validate this expression
            //    .Build();
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            var tenants = await _tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id))
                {
                    var forms = await _appFormsService.GetListAsync(new PagedAndSortedResultRequestDto());
                    foreach (var form in forms.Items)
                    {
                        Logger.LogInformation("TenantName: {tenantName} TenantId: {tenantId} FormId: {formId} FormName: {formName}", tenant.Name, _currentTenant.Id, form.Id, form.ApplicationFormName);
                    }
                }
            }

            Logger.LogInformation("Executed IntakeSyncWorker..!");
            await Task.CompletedTask;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Reporting
{
    public class DynamicViewGeneratorJob(
        IApplicationFormVersionRepository applicationFormVersionRepository,
        ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWorkManager) : AsyncBackgroundJob<DynamicViewGenerationArgs>, ITransientDependency
    {
        public override async Task ExecuteAsync(DynamicViewGenerationArgs args)
        {
            try
            {
                using (currentTenant.Change(args.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(isTransactional: false);
                    var applicationFormVersion = await applicationFormVersionRepository.GetAsync(args.ApplicationFormVersionId);

                    if (applicationFormVersion != null)
                    {
                        var dbContext = await applicationFormVersionRepository.GetDbContextAsync();
                        FormattableString sql = $@"CALL generate_view({args.ApplicationFormVersionId});";
                        await dbContext.Database.ExecuteSqlAsync(sql);
                    }

                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("{errorMessage}", ex.Message);
            }
        }
    }
}

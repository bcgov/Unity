using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Reporting
{
    public class DynamicViewGeneratorHandler(
        IApplicationFormVersionRepository applicationFormVersionRepository,
        ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<DynamicViewGeneratorHandler> logger) : ILocalEventHandler<DynamicViewGenerationEto>, ITransientDependency
    {
        public async Task HandleEventAsync(DynamicViewGenerationEto viewGenerationEvent)
        {
            try
            {
                using (currentTenant.Change(viewGenerationEvent.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(isTransactional: false);
                    var applicationFormVersion = await applicationFormVersionRepository.GetAsync(viewGenerationEvent.ApplicationFormVersionId);

                    if (applicationFormVersion != null)
                    {
                        var dbContext = await applicationFormVersionRepository.GetDbContextAsync();
                        FormattableString sql = $@"CALL generate_view({viewGenerationEvent.ApplicationFormVersionId});";
                        await dbContext.Database.ExecuteSqlAsync(sql);
                    }

                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError("{errorMessage}", ex.Message);
            }
        }
    }
}

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
    public class SubmissionsDynamicViewGeneratorHandler(
        IApplicationFormVersionRepository applicationFormVersionRepository,
        ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<SubmissionsDynamicViewGeneratorHandler> logger) : ILocalEventHandler<SubmissionsDynamicViewGenerationEto>, ITransientDependency
    {
        /// <summary>
        /// Generate a View in the database using the generate_submissions_view function based on the application form version
        /// </summary>
        /// <param name="viewGenerationEvent"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(SubmissionsDynamicViewGenerationEto viewGenerationEvent)
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
                        FormattableString sql = $@"CALL ""Reporting"".generate_submissions_view({viewGenerationEvent.ApplicationFormVersionId});";
                        await dbContext.Database.ExecuteSqlAsync(sql);
                    }

                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ErrorMessage}", ex.Message);
            }
        }
    }
}

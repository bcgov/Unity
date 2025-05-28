using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.Flex.Reporting
{
    public class WorksheetsDynamicViewGeneratorHandler(
            IWorksheetRepository worksheetRepository,
            ICurrentTenant currentTenant,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<WorksheetsDynamicViewGeneratorHandler> logger) : ILocalEventHandler<WorksheetsDynamicViewGeneratorEto>, ITransientDependency
    {
        /// <summary>
        /// Generate a view in the database using the generate_worksheets_view function based on the worksheet
        /// </summary>
        /// <param name="viewGenerationEvent"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(WorksheetsDynamicViewGeneratorEto viewGenerationEvent)
        {            
            try
            {
                using (currentTenant.Change(viewGenerationEvent.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(isTransactional: false);

                    var worksheet = await worksheetRepository.GetAsync(viewGenerationEvent.WorksheetId);

                    if (worksheet != null)
                    {                        
                        var dbContext = await worksheetRepository.GetDbContextAsync();
                        FormattableString sql = $@"CALL ""Reporting"".generate_worksheets_view({viewGenerationEvent.WorksheetId});";
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

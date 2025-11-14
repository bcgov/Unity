using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.Flex.Reporting
{
    public class ScoresheetsDynamicViewGeneratorHandler(
            IScoresheetRepository scoresheetRepository,
            ICurrentTenant currentTenant,
            IUnitOfWorkManager unitOfWorkManager,
            ILogger<ScoresheetsDynamicViewGeneratorHandler> logger) : ILocalEventHandler<ScoresheetsDynamicViewGeneratorEto>, ITransientDependency
    {
        /// <summary>
        /// Generate a view in the database using the generate_scoresheet_view procedure based on the scoresheet
        /// </summary>
        /// <param name="viewGenerationEvent"></param>
        /// <returns></returns>
        public async Task HandleEventAsync(ScoresheetsDynamicViewGeneratorEto viewGenerationEvent)
        {
            try
            {
                using (currentTenant.Change(viewGenerationEvent.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(isTransactional: false);

                    var scoresheet = await scoresheetRepository.GetAsync(viewGenerationEvent.ScoresheetId);

                    if (scoresheet != null)
                    {
                        var dbContext = await scoresheetRepository.GetDbContextAsync();
                        
                        // Find the application form ID (correlation_id) from the ReportColumnsMap
                        // The correlation ID in the map should be the application form ID for scoresheet provider
                        var correlationIdQuery = @"
                            SELECT ""CorrelationId"" 
                            FROM ""Reporting"".""ReportColumnsMaps"" 
                            WHERE ""CorrelationId"" IN (
                                SELECT ""Id""
                                FROM ""ApplicationForms""
                                WHERE ""ScoresheetId"" = {0}
                            )
                            AND ""CorrelationProvider"" = 'scoresheet'
                            LIMIT 1";
                        
                        var correlationIdResult = await dbContext.Database
                            .SqlQueryRaw<Guid>(correlationIdQuery, scoresheet.Id)
                            .FirstOrDefaultAsync();

                        if (correlationIdResult != Guid.Empty)
                        {
                            // Use the procedure with correlation_id (which is the application form ID)
                            FormattableString sql = $@"CALL ""Reporting"".generate_scoresheet_view({correlationIdResult});";
                            await dbContext.Database.ExecuteSqlAsync(sql);
                        }
                        else
                        {
                            logger.LogWarning("No application form found for scoresheet ID: {ScoresheetId}", scoresheet.Id);
                        }
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

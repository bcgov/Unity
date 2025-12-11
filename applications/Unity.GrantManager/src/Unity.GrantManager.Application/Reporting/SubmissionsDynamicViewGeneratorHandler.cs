using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Notifications;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Reporting
{
    public class SubmissionsDynamicViewGeneratorHandler(
        IApplicationFormVersionRepository applicationFormVersionRepository,
        ICurrentTenant currentTenant,        
        INotificationsAppService notificationsAppService,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<SubmissionsDynamicViewGeneratorHandler> logger) : AsyncBackgroundJob<SubmissionsDynamicViewGenerationArgs>, ITransientDependency
    {
        /// <summary>
        /// Generate a View in the database using the generate_submissions_view function based on the application form version
        /// </summary>
        /// <param name="viewGenerationEvent"></param>
        /// <returns></returns>
        public override async Task ExecuteAsync(SubmissionsDynamicViewGenerationArgs viewGenerationEvent)
        {
            try
            {
                using (currentTenant.Change(viewGenerationEvent.TenantId))
                {
                    using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
                    
                    var applicationFormVersion = await applicationFormVersionRepository.GetAsync(viewGenerationEvent.ApplicationFormVersionId);

                    if (applicationFormVersion != null)
                    {
                        await GenerateSubmissionsViewAsync(viewGenerationEvent.ApplicationFormVersionId);
                    }
                    
                    await uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{ErrorMessage}", ex.Message);
                await NotifyTeamsAsync(viewGenerationEvent, ex);
            }
        }

        private async Task GenerateSubmissionsViewAsync(Guid applicationFormVersionId)
        {
            var dbContext = await applicationFormVersionRepository.GetDbContextAsync();
            
            FormattableString sql = $@"CALL ""Reporting"".generate_submissions_view({applicationFormVersionId});";
            await dbContext.Database.ExecuteSqlAsync(sql);
        }

        private async Task NotifyTeamsAsync(SubmissionsDynamicViewGenerationArgs viewGenerationEvent, Exception exception)
        {
            try
            {
                var facts = new List<Fact>
                {
                    new() { Name = "FormVersionId", Value = viewGenerationEvent.ApplicationFormVersionId.ToString() },
                    new() { Name = "TenantId", Value = viewGenerationEvent.TenantId?.ToString() ?? "N/A" },
                    new() { Name = "Error", Value = exception.Message }
                };

                var activityTitle = "Reporting view generation failed";
                var activitySubtitle = $"Form version {viewGenerationEvent.ApplicationFormVersionId}";

                await notificationsAppService.PostToTeamsAsync(activityTitle, activitySubtitle, facts);
            }
            catch (Exception notifyEx)
            {
                logger.LogWarning(notifyEx, "Failed to post Teams notification for view generation failure on form version {FormVersionId}", viewGenerationEvent.ApplicationFormVersionId);
            }
        }
    }
}

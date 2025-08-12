using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Locality.BackgroundJobs
{
    public class RetrofillElectoralDistrictsBackgroundJob(
       IApplicationRepository applicationRepository,
       IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
       IApplicationFormVersionRepository applicationFormVersionRepository,
       ILocalEventBus localEventBus,
       ICurrentTenant currentTenant,
       IUnitOfWorkManager unitOfWorkManager,
       ILogger<RetrofillElectoralDistrictsBackgroundJob> logger) : AsyncBackgroundJob<RetrofillElectoralDistrictsBackgroundJobArgs>, ITransientDependency
    {
        private const string LogPrefix = "[ElectoralRetroFill]";

        public override async Task ExecuteAsync(RetrofillElectoralDistrictsBackgroundJobArgs args)
        {
            LogPrefixedInfo($"Executing electoral district retrofill for {args.TenantId}");

            using (currentTenant.Change(args.TenantId))
            {
                try
                {
                    // Read all the applications for the tenant, get a list of their Id's                    
                    var applicationIds = (await applicationRepository
                            .GetListAsync())
                            .Select(s => s.Id);

                    // Foreach one we read again individually, and commit indivudally and log per record
                    foreach (var applicationId in applicationIds)
                    {
                        try
                        {
                            using var unitOfWork = unitOfWorkManager.Begin(true);

                            LogPrefixedInfo($"Processing applicationId {applicationId}");

                            var application = await (await applicationRepository.GetQueryableAsync())
                                    .Include(s => s.Applicant)
                                        .ThenInclude(s => s.ApplicantAddresses)
                                    .Include(s => s.ApplicationForm)
                                    .FirstOrDefaultAsync(s => s.Id == applicationId);

                            var submission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
                            var formVersionId = submission?.ApplicationFormVersionId;

                            if (formVersionId == null)
                            {
                                LogPrefixedInfo($"No form version found for applicationId {applicationId}, skipping retrofill.");
                                continue;
                            }

                            var formVersion = await applicationFormVersionRepository.GetAsync(formVersionId.Value);

                            await localEventBus.PublishAsync(new ApplicationProcessEvent
                            {
                                Application = application,
                                FormVersion = formVersion,
                                ApplicationFormSubmission = null,
                                RawSubmission = null,
                                OnlyLocationRetrofill = true
                            });

                            await unitOfWork.CompleteAsync();

                            // To avoid any rate limiting issues with any external services, we add a small delay
                            await Task.Delay(500);
                        }
                        catch (Exception ex)
                        {
                            LogPrefixedError(ex, $"Error executing electoral district retrofill for applicationId: {applicationId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogPrefixedError(ex, $"Error executing electoral district retrofill for tenantId: {args.TenantId}");
                }
            }
        }

        private void LogPrefixedInfo(string message)
        {
            logger.LogInformation("{Prefix} {Message}", LogPrefix, message);
        }

        private void LogPrefixedError(Exception ex, string message)
        {
            logger.LogError(ex, "{Prefix} {Message}", LogPrefix, message);
        }
    }
}

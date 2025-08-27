using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[ExposeServices(typeof(ApplicationLinksAppService), typeof(IApplicationLinksService))]
public class ApplicationLinksAppService : CrudAppService<
        ApplicationLink,
        ApplicationLinksDto,
        Guid>, IApplicationLinksService
{
    public IApplicationLinksRepository ApplicationLinksRepository { get; set; } = null!;
    public IApplicationRepository ApplicationRepository { get; set; } = null!;
    public IApplicationFormRepository ApplicationFormRepository { get; set; } = null!;
    public IApplicantRepository ApplicantRepository { get; set; } = null!;

    // Constructor for the required repository
    public ApplicationLinksAppService(IRepository<ApplicationLink, Guid> repository) : base(repository) { }

    public async Task<List<ApplicationLinksInfoDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var applicationLinksQuery = await ApplicationLinksRepository.GetQueryableAsync();
        var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
        var applicationFormsQuery = await ApplicationFormRepository.GetQueryableAsync();
        var applicantsQuery = await ApplicantRepository.GetQueryableAsync();

        var combinedQuery = from applicationLinks in applicationLinksQuery
                            join application in applicationsQuery on applicationLinks.LinkedApplicationId equals application.Id into appLinks
                            from application in appLinks.DefaultIfEmpty() // Left join for safety
                            join appForm in applicationFormsQuery on application.ApplicationFormId equals appForm.Id into appForms
                            from appForm in appForms.DefaultIfEmpty() // Left join for safety
                            join applicant in applicantsQuery on application.ApplicantId equals applicant.Id into applicants
                            from applicant in applicants.DefaultIfEmpty() // Left join for safety
                            where applicationLinks.ApplicationId == applicationId || applicationLinks.LinkedApplicationId == applicationId
                            select new ApplicationLinksInfoDto
                            {
                                Id = applicationLinks.Id,
                                ApplicationId = application.Id,
                                ApplicationStatus = application.ApplicationStatus.InternalStatus,
                                ReferenceNumber = application.ReferenceNo,
                                Category = appForm.Category ?? "Unknown", // Handle potential nulls
                                ProjectName = application.ProjectName,
                                ApplicantName = applicant.ApplicantName ?? "Unknown", // Handle potential nulls
                                LinkType = applicationLinks.LinkType
                            };

        return await combinedQuery.ToListAsync();
    }

    public async Task<ApplicationLinksInfoDto> GetLinkedApplicationAsync(Guid currentApplicationId, Guid linkedApplicationId)
    {
        var applicationLinksQuery = await ApplicationLinksRepository.GetQueryableAsync();
        var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
        var applicationFormsQuery = await ApplicationFormRepository.GetQueryableAsync();
        var applicantsQuery = await ApplicantRepository.GetQueryableAsync();

        var combinedQuery = from applicationLinks in applicationLinksQuery
                            join application in applicationsQuery on applicationLinks.LinkedApplicationId equals application.Id into appLinks
                            from application in appLinks.DefaultIfEmpty() // Left join for safety
                            join appForm in applicationFormsQuery on application.ApplicationFormId equals appForm.Id into appForms
                            from appForm in appForms.DefaultIfEmpty() // Left join for safety
                            join applicant in applicantsQuery on application.ApplicantId equals applicant.Id into applicants
                            from applicant in applicants.DefaultIfEmpty() // Left join for safety
                            where applicationLinks.ApplicationId == linkedApplicationId && applicationLinks.LinkedApplicationId == currentApplicationId
                            select new ApplicationLinksInfoDto
                            {
                                Id = applicationLinks.Id,
                                ApplicationId = application.Id,
                                ApplicationStatus = application.ApplicationStatus.InternalStatus,
                                ReferenceNumber = application.ReferenceNo,
                                Category = appForm.Category ?? "Unknown", // Handle potential nulls
                                ProjectName = application.ProjectName,
                                ApplicantName = applicant.ApplicantName ?? "Unknown", // Handle potential nulls
                                LinkType = applicationLinks.LinkType
                            };

        return await combinedQuery.SingleAsync();
    }

    public async Task<ApplicationLinksInfoDto> GetCurrentApplicationInfoAsync(Guid applicationId)
    {
        Logger.LogInformation("GetCurrentApplicationInfoAsync called with applicationId: {ApplicationId}", applicationId);
        
        try
        {
            var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
            var application = await applicationsQuery
                .Include(a => a.ApplicationStatus)
                .Where(a => a.Id == applicationId)
                .FirstOrDefaultAsync();
                
            if (application == null)
            {
                Logger.LogWarning("Application not found with ID: {ApplicationId}", applicationId);
                return new ApplicationLinksInfoDto
                {
                    Id = Guid.Empty,
                    ApplicationId = applicationId,
                    ApplicationStatus = "Not Found",
                    ReferenceNumber = "Unknown",
                    Category = "Unknown",
                    ProjectName = "Unknown",
                    ApplicantName = "Unknown",
                    LinkType = ApplicationLinkType.Related
                };
            }


            // Now try to get related data safely
            string category = "Unknown";
            string applicantName = "Unknown";

            try
            {
                var applicationFormsQuery = await ApplicationFormRepository.GetQueryableAsync();
                var applicationForm = await applicationFormsQuery
                    .Where(af => af.Id == application.ApplicationFormId)
                    .FirstOrDefaultAsync();
                if (applicationForm != null)
                {
                    category = applicationForm.Category ?? "Unknown";
                }
                else
                {
                    Logger.LogWarning("Application form not found with ID: {ApplicationFormId}", application.ApplicationFormId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error looking up application form with ID: {ApplicationFormId}", application.ApplicationFormId);
            }

            try
            {
                var applicantsQuery = await ApplicantRepository.GetQueryableAsync();
                var applicant = await applicantsQuery
                    .Where(ap => ap.Id == application.ApplicantId)
                    .FirstOrDefaultAsync();
                if (applicant != null)
                {
                    applicantName = applicant.ApplicantName ?? "Unknown";
                }
                else
                {
                    Logger.LogWarning("Applicant not found with ID: {ApplicantId}", application.ApplicantId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error looking up applicant with ID: {ApplicantId}", application.ApplicantId);
            }

            // Get application status (loaded via Include)
            string applicationStatus = "Unknown";
            try
            {
                if (application.ApplicationStatus != null)
                {
                    applicationStatus = application.ApplicationStatus.InternalStatus ?? "Unknown";
                }
                else
                {
                    Logger.LogWarning("ApplicationStatus is null for application {ApplicationId}", applicationId);
                    applicationStatus = "Status Not Available";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error accessing ApplicationStatus for application {ApplicationId}", applicationId);
                applicationStatus = "Status Unavailable";
            }

            var result = new ApplicationLinksInfoDto
            {
                Id = Guid.Empty,
                ApplicationId = application.Id,
                ApplicationStatus = applicationStatus,
                ReferenceNumber = application.ReferenceNo ?? "Unknown",
                Category = category,
                ProjectName = application.ProjectName ?? "Unknown",
                ApplicantName = applicantName,
                LinkType = ApplicationLinkType.Related
            };
            
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Critical error in GetCurrentApplicationInfoAsync for applicationId: {ApplicationId}", applicationId);
            
            // If all else fails, return a basic structure
            return new ApplicationLinksInfoDto
            {
                Id = Guid.Empty,
                ApplicationId = applicationId,
                ApplicationStatus = "Error Loading",
                ReferenceNumber = "Unknown",
                Category = "Unknown",
                ProjectName = "Unknown",
                ApplicantName = "Unknown",
                LinkType = ApplicationLinkType.Related
            };
        }
    }

    public async Task DeleteWithPairAsync(Guid applicationLinkId)
    {
        // Get the link to find the paired record
        var link = await Repository.GetAsync(applicationLinkId);
        
        // Find the paired link (reverse direction)
        var applicationLinksQuery = await ApplicationLinksRepository.GetQueryableAsync();
        var pairedLink = await applicationLinksQuery
            .Where(x => x.ApplicationId == link.LinkedApplicationId && x.LinkedApplicationId == link.ApplicationId)
            .FirstOrDefaultAsync();
        
        // Delete both links
        await Repository.DeleteAsync(applicationLinkId);
        
        if (pairedLink != null)
        {
            await Repository.DeleteAsync(pairedLink.Id);
        }
    }

    public async Task<ApplicationLinksInfoDto> GetApplicationDetailsByReferenceAsync(string referenceNumber)
    {
        Logger.LogInformation("GetApplicationDetailsByReferenceAsync called with referenceNumber: {ReferenceNumber}", referenceNumber);
        
        try
        {
            var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
            var application = await applicationsQuery
                .Include(a => a.ApplicationStatus)
                .Where(a => a.ReferenceNo == referenceNumber)
                .FirstOrDefaultAsync();
                
            if (application == null)
            {
                Logger.LogWarning("Application not found with ReferenceNumber: {ReferenceNumber}", referenceNumber);
                return new ApplicationLinksInfoDto
                {
                    Id = Guid.Empty,
                    ApplicationId = Guid.Empty,
                    ApplicationStatus = "Not Found",
                    ReferenceNumber = referenceNumber,
                    Category = "Unknown",
                    ProjectName = "Unknown",
                    ApplicantName = "Unknown",
                    LinkType = ApplicationLinkType.Related
                };
            }

            // Get related data safely
            string category = "Unknown";
            string applicantName = "Unknown";

            try
            {
                var applicationFormsQuery = await ApplicationFormRepository.GetQueryableAsync();
                var applicationForm = await applicationFormsQuery
                    .Where(af => af.Id == application.ApplicationFormId)
                    .FirstOrDefaultAsync();
                if (applicationForm != null)
                {
                    category = applicationForm.Category ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error looking up application form");
            }

            try
            {
                var applicantsQuery = await ApplicantRepository.GetQueryableAsync();
                var applicant = await applicantsQuery
                    .Where(ap => ap.Id == application.ApplicantId)
                    .FirstOrDefaultAsync();
                if (applicant != null)
                {
                    applicantName = applicant.ApplicantName ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error looking up applicant");
            }

            string applicationStatus = "Unknown";
            if (application.ApplicationStatus != null)
            {
                applicationStatus = application.ApplicationStatus.InternalStatus ?? "Unknown";
            }

            return new ApplicationLinksInfoDto
            {
                Id = Guid.Empty,
                ApplicationId = application.Id,
                ApplicationStatus = applicationStatus,
                ReferenceNumber = application.ReferenceNo ?? referenceNumber,
                Category = category,
                ProjectName = application.ProjectName ?? "Unknown",
                ApplicantName = applicantName,
                LinkType = ApplicationLinkType.Related
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Critical error in GetApplicationDetailsByReferenceAsync for referenceNumber: {ReferenceNumber}", referenceNumber);
            
            return new ApplicationLinksInfoDto
            {
                Id = Guid.Empty,
                ApplicationId = Guid.Empty,
                ApplicationStatus = "Error Loading",
                ReferenceNumber = referenceNumber,
                Category = "Unknown",
                ProjectName = "Unknown",
                ApplicantName = "Unknown",
                LinkType = ApplicationLinkType.Related
            };
        }
    }

    public async Task UpdateLinkTypeAsync(Guid applicationLinkId, ApplicationLinkType newLinkType)
    {
        Logger.LogInformation("UpdateLinkTypeAsync called with linkId: {LinkId}, newLinkType: {LinkType}", applicationLinkId, newLinkType);
        
        try
        {
            // Get the existing link
            var link = await Repository.GetAsync(applicationLinkId);
            
            if (link != null)
            {
                // Update the link type
                link.LinkType = newLinkType;
                await Repository.UpdateAsync(link);
                
                Logger.LogInformation("Successfully updated link type for linkId: {LinkId}", applicationLinkId);
            }
            else
            {
                Logger.LogWarning("Link not found with ID: {LinkId}", applicationLinkId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating link type for linkId: {LinkId}", applicationLinkId);
            throw;
        }
    }
}
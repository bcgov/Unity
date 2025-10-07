using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.ApplicationForms;
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
    public IApplicantRepository ApplicantRepository { get; set; } = null!;
    public IApplicationFormAppService ApplicationFormAppService { get; set; } = null!;
    
    public ApplicationLinksAppService(IRepository<ApplicationLink, Guid> repository) : base(repository) { }

    public async Task<List<ApplicationLinksInfoDto>> GetListByApplicationAsync(Guid applicationId)
    {
        var applicationLinksQuery = await ApplicationLinksRepository.GetQueryableAsync();
        var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
        var applicantsQuery = await ApplicantRepository.GetQueryableAsync();

        // Get basic application and applicant data without form details
        var basicQuery = from applicationLinks in applicationLinksQuery
                        join application in applicationsQuery on applicationLinks.LinkedApplicationId equals application.Id into appLinks
                        from application in appLinks.DefaultIfEmpty() // Left join for safety
                        join applicant in applicantsQuery on application.ApplicantId equals applicant.Id into applicants
                        from applicant in applicants.DefaultIfEmpty() // Left join for safety
                        where applicationLinks.ApplicationId == applicationId || applicationLinks.LinkedApplicationId == applicationId
                        select new
                        {
                            Id = applicationLinks.Id,
                            ApplicationId = application.Id,
                            ApplicationStatus = application.ApplicationStatus.InternalStatus,
                            ReferenceNumber = application.ReferenceNo,
                            ProjectName = application.ProjectName,
                            ApplicantName = applicant.ApplicantName ?? GrantManagerConsts.UnknownValue,
                            LinkType = applicationLinks.LinkType
                        };

        var basicResults = await basicQuery.ToListAsync();
        var resultList = new List<ApplicationLinksInfoDto>();

        // For each application, get the form details using the service
        foreach (var basicResult in basicResults)
        {
            string category = GrantManagerConsts.UnknownValue;
            int? formVersion = null;

            try
            {
                var formDetails = await ApplicationFormAppService.GetFormDetailsByApplicationIdAsync(basicResult.ApplicationId);
                category = formDetails.ApplicationFormCategory ?? GrantManagerConsts.UnknownValue;
                formVersion = formDetails.ApplicationFormVersion;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to get form details for application {ApplicationId}", basicResult.ApplicationId);
            }

            resultList.Add(new ApplicationLinksInfoDto
            {
                Id = basicResult.Id,
                ApplicationId = basicResult.ApplicationId,
                ApplicationStatus = basicResult.ApplicationStatus,
                ReferenceNumber = basicResult.ReferenceNumber,
                Category = category,
                ProjectName = basicResult.ProjectName,
                ApplicantName = basicResult.ApplicantName,
                LinkType = basicResult.LinkType,
                FormVersion = formVersion
            });
        }

        return resultList;
    }

    public async Task<ApplicationLinksInfoDto> GetLinkedApplicationAsync(Guid currentApplicationId, Guid linkedApplicationId)
    {
        var applicationLinksQuery = await ApplicationLinksRepository.GetQueryableAsync();
        var applicationsQuery = await ApplicationRepository.GetQueryableAsync();
        var applicantsQuery = await ApplicantRepository.GetQueryableAsync();

        // Get basic application and applicant data without form details
        var basicQuery = from applicationLinks in applicationLinksQuery
                        join application in applicationsQuery on applicationLinks.LinkedApplicationId equals application.Id into appLinks
                        from application in appLinks.DefaultIfEmpty() // Left join for safety
                        join applicant in applicantsQuery on application.ApplicantId equals applicant.Id into applicants
                        from applicant in applicants.DefaultIfEmpty() // Left join for safety
                        where applicationLinks.ApplicationId == linkedApplicationId && applicationLinks.LinkedApplicationId == currentApplicationId
                        select new
                        {
                            Id = applicationLinks.Id,
                            ApplicationId = application.Id,
                            ApplicationStatus = application.ApplicationStatus.InternalStatus,
                            ReferenceNumber = application.ReferenceNo,
                            ProjectName = application.ProjectName,
                            ApplicantName = applicant.ApplicantName ?? GrantManagerConsts.UnknownValue,
                            LinkType = applicationLinks.LinkType
                        };

        var basicResult = await basicQuery.SingleAsync();

        // Get form details using the service
        string category = GrantManagerConsts.UnknownValue;
        int? formVersion = null;

        try
        {
            var formDetails = await ApplicationFormAppService.GetFormDetailsByApplicationIdAsync(basicResult.ApplicationId);
            category = formDetails.ApplicationFormCategory ?? GrantManagerConsts.UnknownValue;
            formVersion = formDetails.ApplicationFormVersion;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get form details for application {ApplicationId}", basicResult.ApplicationId);
        }

        return new ApplicationLinksInfoDto
        {
            Id = basicResult.Id,
            ApplicationId = basicResult.ApplicationId,
            ApplicationStatus = basicResult.ApplicationStatus,
            ReferenceNumber = basicResult.ReferenceNumber,
            Category = category,
            ProjectName = basicResult.ProjectName,
            ApplicantName = basicResult.ApplicantName,
            LinkType = basicResult.LinkType,
            FormVersion = formVersion
        };
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
                    ReferenceNumber = GrantManagerConsts.UnknownValue,
                    Category = GrantManagerConsts.UnknownValue,
                    ProjectName = GrantManagerConsts.UnknownValue,
                    ApplicantName = GrantManagerConsts.UnknownValue,
                    LinkType = ApplicationLinkType.Related,
                    FormVersion = null
                };
            }


            // Now try to get related data safely
            string category = GrantManagerConsts.UnknownValue;
            string applicantName = GrantManagerConsts.UnknownValue;
            int? formVersion = null;

            try
            {
                var formDetails = await ApplicationFormAppService.GetFormDetailsByApplicationIdAsync(applicationId);
                category = formDetails.ApplicationFormCategory ?? GrantManagerConsts.UnknownValue;
                formVersion = formDetails.ApplicationFormVersion;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error looking up form details for application ID: {ApplicationId}", applicationId);
            }

            try
            {
                var applicantsQuery = await ApplicantRepository.GetQueryableAsync();
                var applicant = await applicantsQuery
                    .Where(ap => ap.Id == application.ApplicantId)
                    .FirstOrDefaultAsync();
                if (applicant != null)
                {
                    applicantName = applicant.ApplicantName ?? GrantManagerConsts.UnknownValue;
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
            string applicationStatus;
            try
            {
                if (application.ApplicationStatus != null)
                {
                    applicationStatus = application.ApplicationStatus.InternalStatus ?? GrantManagerConsts.UnknownValue;
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
                ReferenceNumber = application.ReferenceNo ?? GrantManagerConsts.UnknownValue,
                Category = category,
                ProjectName = application.ProjectName ?? GrantManagerConsts.UnknownValue,
                ApplicantName = applicantName,
                LinkType = ApplicationLinkType.Related,
                FormVersion = formVersion
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
                ReferenceNumber = GrantManagerConsts.UnknownValue,
                Category = GrantManagerConsts.UnknownValue,
                ProjectName = GrantManagerConsts.UnknownValue,
                ApplicantName = GrantManagerConsts.UnknownValue,
                LinkType = ApplicationLinkType.Related,
                FormVersion = null
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
                    Category = GrantManagerConsts.UnknownValue,
                    ProjectName = GrantManagerConsts.UnknownValue,
                    ApplicantName = GrantManagerConsts.UnknownValue,
                    LinkType = ApplicationLinkType.Related,
                    FormVersion = null
                };
            }

            // Get related data safely
            string category = GrantManagerConsts.UnknownValue;
            string applicantName = GrantManagerConsts.UnknownValue;
            int? formVersion = null;

            try
            {
                var formDetails = await ApplicationFormAppService.GetFormDetailsByApplicationIdAsync(application.Id);
                category = formDetails.ApplicationFormCategory ?? GrantManagerConsts.UnknownValue;
                formVersion = formDetails.ApplicationFormVersion;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error looking up form details for application ID: {ApplicationId}", application.Id);
            }

            try
            {
                var applicantsQuery = await ApplicantRepository.GetQueryableAsync();
                var applicant = await applicantsQuery
                    .Where(ap => ap.Id == application.ApplicantId)
                    .FirstOrDefaultAsync();
                if (applicant != null)
                {
                    applicantName = applicant.ApplicantName ?? GrantManagerConsts.UnknownValue;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error looking up applicant");
            }

            string applicationStatus = GrantManagerConsts.UnknownValue;
            if (application.ApplicationStatus != null)
            {
                applicationStatus = application.ApplicationStatus.InternalStatus ?? GrantManagerConsts.UnknownValue;
            }

            return new ApplicationLinksInfoDto
            {
                Id = Guid.Empty,
                ApplicationId = application.Id,
                ApplicationStatus = applicationStatus,
                ReferenceNumber = application.ReferenceNo ?? referenceNumber,
                Category = category,
                ProjectName = application.ProjectName ?? GrantManagerConsts.UnknownValue,
                ApplicantName = applicantName,
                LinkType = ApplicationLinkType.Related,
                FormVersion = formVersion
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
                Category = GrantManagerConsts.UnknownValue,
                ProjectName = GrantManagerConsts.UnknownValue,
                ApplicantName = GrantManagerConsts.UnknownValue,
                LinkType = ApplicationLinkType.Related,
                FormVersion = null
            };
        }
    }

    public async Task UpdateLinkTypeAsync(Guid applicationLinkId, ApplicationLinkType newLinkType)
    {
        Logger.LogInformation("UpdateLinkTypeAsync called with linkId: {LinkId}, newLinkType: {LinkType}", applicationLinkId, newLinkType);
                
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

    public async Task<ApplicationLinkValidationResult> ValidateApplicationLinksAsync(
        Guid currentApplicationId, 
        List<ApplicationLinkValidationRequest> proposedLinks)
    {
        var result = new ApplicationLinkValidationResult();
        
        // Skip validation for empty or Related-only links
        var hierarchicalLinks = proposedLinks.Where(l => l.LinkType != ApplicationLinkType.Related).ToList();
        if (hierarchicalLinks.Count == 0)
        {
            return result;
        }
        
        // Validate current app constraints
        var currentAppError = GetCurrentAppConstraintError(hierarchicalLinks);
        
        // Process each proposed link
        foreach (var proposedLink in hierarchicalLinks)
        {
            var errorMessage = await GetLinkValidationError(currentApplicationId, proposedLink, currentAppError, hierarchicalLinks);
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                result.ValidationErrors[proposedLink.ReferenceNumber] = true;
                result.ErrorMessages[proposedLink.ReferenceNumber] = errorMessage;
            }
            else
            {
                result.ValidationErrors[proposedLink.ReferenceNumber] = false;
            }
        }
        
        return result;
    }
    
    private static string GetCurrentAppConstraintError(List<ApplicationLinkValidationRequest> proposedLinks)
    {
        var parentCount = proposedLinks.Count(l => l.LinkType == ApplicationLinkType.Parent);
        var hasParent = proposedLinks.Exists(l => l.LinkType == ApplicationLinkType.Parent); 
        var hasChild = proposedLinks.Exists(l => l.LinkType == ApplicationLinkType.Child); 

        if (parentCount > 1)
        {
            return "Error: A submission can not have two parents. Please revise the link type.";
        }
        
        if (hasParent && hasChild)
        {
            return "Error: Cannot add a parent link. This application already has children.";
        }
        
        return string.Empty;
    }
    
    private async Task<string> GetLinkValidationError(
        Guid currentApplicationId, 
        ApplicationLinkValidationRequest proposedLink,
        string currentAppError,
        List<ApplicationLinkValidationRequest> allProposedLinks)
    {
        switch (proposedLink.LinkType)
        {
            case ApplicationLinkType.Parent:
                // First check current app constraints
                if (!string.IsNullOrEmpty(currentAppError))
                {
                    return currentAppError;
                }
                
                // Then check if the proposed parent is already a child of another app
                return await GetParentTargetValidationError(currentApplicationId, proposedLink);
                
            case ApplicationLinkType.Child:
                // Check if current app is trying to be both parent and child
                if (!string.IsNullOrEmpty(currentAppError) && allProposedLinks.Exists(l => l.LinkType == ApplicationLinkType.Parent))
                {
                    return "Error: This application is already a child of another application. An application cannot be both a parent and a child.";
                }
                
                // Check target app conflicts
                return await GetChildTargetValidationError(currentApplicationId, proposedLink);
                
            default:
                return string.Empty;
        }
    }
    
    private async Task<string> GetParentTargetValidationError(Guid currentApplicationId, ApplicationLinkValidationRequest proposedLink)
    {
        var targetLinks = await GetListByApplicationAsync(proposedLink.TargetApplicationId);

        // Exclude reverse links and self-references
        var targetExternalLinks = targetLinks.Where(l =>
            l.ApplicationId != currentApplicationId &&
            l.ApplicationId != proposedLink.TargetApplicationId).ToList();

        if (targetExternalLinks.Exists(l => l.LinkType == ApplicationLinkType.Parent))
        {
            return "Error: The selected submission is already a child of another application. An application cannot be both a parent and a child.";
        }
        
        return string.Empty;
    }
    
    private async Task<string> GetChildTargetValidationError(Guid currentApplicationId, ApplicationLinkValidationRequest proposedLink)
    {
        var targetLinks = await GetListByApplicationAsync(proposedLink.TargetApplicationId);

        // Exclude reverse links and self-references
        var targetExternalLinks = targetLinks.Where(l => 
            l.ApplicationId != currentApplicationId && 
            l.ApplicationId != proposedLink.TargetApplicationId).ToList();
        
        if (targetExternalLinks.Exists(l => l.LinkType == ApplicationLinkType.Parent))
        {
            return "Error: The selected submission already has a parent. A submission cannot have multiple parents.";
        }
        
        if (targetExternalLinks.Exists(l => l.LinkType == ApplicationLinkType.Child))
        {
            return "Error: The selected submission is already a parent to other applications. An application cannot be both a parent and a child.";
        }
        
        return string.Empty;
    }
}
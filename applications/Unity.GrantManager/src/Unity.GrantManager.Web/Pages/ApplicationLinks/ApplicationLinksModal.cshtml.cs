using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationLinks
{
    public class LinkWithType
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public ApplicationLinkType LinkType { get; set; } = ApplicationLinkType.Related;
    }

    public class LinkValidationRequestDto
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public ApplicationLinkType LinkType { get; set; }
    }
}

namespace Unity.GrantManager.Web.Pages.ApplicationLinks
{
    public class ApplicationLinksModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("")]
        public string? SelectedApplications { get; set; } = string.Empty;

        [BindProperty]
        public string? AllApplications { get; set; } = string.Empty;

        [BindProperty]
        public string? GrantApplicationsList { get; set; } = string.Empty;

        [BindProperty]
        public string? LinkedApplicationsList { get; set; } = string.Empty;

        [BindProperty]
        public string? SelectedApplicationId { get; set; } = string.Empty;

        [BindProperty]
        public string? ActionType { get; set; } = string.Empty;

        private readonly IApplicationLinksService _applicationLinksService;

        private readonly IGrantApplicationAppService _grantApplicationAppService;


        [BindProperty]
        public string? CommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? UncommonTags { get; set; } = string.Empty;

        [BindProperty]
        public string? Tags { get; set; } = string.Empty;

        [BindProperty]
        public Guid? CurrentApplicationId { get; set; }

        [BindProperty]
        public string? LinksWithTypes { get; set; } = string.Empty;

        public ApplicationLinksInfoDto? CurrentApplication { get; set; }

        public ApplicationLinksModalModel(IApplicationLinksService applicationLinksService, IGrantApplicationAppService grantApplicationAppService)
        {
            _applicationLinksService = applicationLinksService ?? throw new ArgumentNullException(nameof(applicationLinksService));
            _grantApplicationAppService = grantApplicationAppService ?? throw new ArgumentNullException(nameof(grantApplicationAppService));
        }

        public async Task OnGetAsync(Guid applicationId)
        {
            try
            {
                CurrentApplicationId = applicationId;
                
                // Get current application info for display
                CurrentApplication = await _applicationLinksService.GetCurrentApplicationInfoAsync(applicationId);
                
                var grantApplications = await _grantApplicationAppService.GetAllApplicationsAsync();
                var tempGrantApplications = new List<GrantApplicationLiteDto>(grantApplications);
                var currentApplication = tempGrantApplications.Single(item => item.Id == applicationId);

                var linkedApplications = await _applicationLinksService.GetListByApplicationAsync(applicationId);
                var filteredLinkedApplications = linkedApplications.Where(item => item.ApplicationId != CurrentApplicationId);

                // remove current application id from ths suggestion list
                tempGrantApplications.Remove(currentApplication);

                var formattedAllApplications = tempGrantApplications.Select(item => item.ReferenceNo + " - " + item.ApplicantName).ToList();
                var formattedLinkedApplications = filteredLinkedApplications.Select(item => item.ReferenceNumber + " - " + item.ProjectName).ToList();

                AllApplications = string.Join(",", formattedAllApplications);
                SelectedApplications = string.Join(",", formattedLinkedApplications);
                GrantApplicationsList = JsonConvert.SerializeObject(grantApplications);
                LinkedApplicationsList = JsonConvert.SerializeObject(filteredLinkedApplications);

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error loading application links modal data");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(LinksWithTypes))
                {
                    List<LinkWithType>? selectedLinksWithTypes = JsonConvert.DeserializeObject<List<LinkWithType>>(LinksWithTypes);
                    List<GrantApplicationLiteDto>? grantApplications = JsonConvert.DeserializeObject<List<GrantApplicationLiteDto>>(GrantApplicationsList!);
                    List<ApplicationLinksInfoDto>? linkedApplications = JsonConvert.DeserializeObject<List<ApplicationLinksInfoDto>>(LinkedApplicationsList!);

                    if (selectedLinksWithTypes != null && grantApplications != null && linkedApplications != null)
                    {
                        // Add new links and update existing ones
                        foreach (var linkWithType in selectedLinksWithTypes)
                        {
                            var existingLink = linkedApplications.Find(app => app.ReferenceNumber == linkWithType.ReferenceNumber);
                            
                            if (existingLink == null)
                            {
                                // Add new link
                                var targetApplication = grantApplications.Find(app => app.ReferenceNo == linkWithType.ReferenceNumber);
                                if (targetApplication != null)
                                {
                                    var linkedApplicationId = targetApplication.Id;

                                    // For CurrentApplication -> LinkedApplication
                                    await _applicationLinksService.CreateAsync(new ApplicationLinksDto
                                    {
                                        ApplicationId = CurrentApplicationId ?? Guid.Empty,
                                        LinkedApplicationId = linkedApplicationId,
                                        LinkType = linkWithType.LinkType
                                    });

                                    // For LinkedApplication -> CurrentApplication (reverse link with appropriate type)
                                    var reverseLinkType = GetReverseLinkType(linkWithType.LinkType);
                                    await _applicationLinksService.CreateAsync(new ApplicationLinksDto
                                    {
                                        ApplicationId = linkedApplicationId,
                                        LinkedApplicationId = CurrentApplicationId ?? Guid.Empty,
                                        LinkType = reverseLinkType
                                    });
                                }
                            }
                            else
                            {
                                // Check if the link type has changed
                                if (existingLink.LinkType != linkWithType.LinkType)
                                {
                                    // Update the existing link's type
                                    await _applicationLinksService.UpdateLinkTypeAsync(existingLink.Id, linkWithType.LinkType);
                                    
                                    // Also update the reverse link
                                    var reverseLink = await _applicationLinksService.GetLinkedApplicationAsync(CurrentApplicationId ?? Guid.Empty, existingLink.ApplicationId);
                                    var reverseLinkType = GetReverseLinkType(linkWithType.LinkType);
                                    await _applicationLinksService.UpdateLinkTypeAsync(reverseLink.Id, reverseLinkType);
                                    
                                    Logger.LogInformation("Updated link type for {ReferenceNumber} from {OldType} to {NewType}", 
                                        linkWithType.ReferenceNumber, existingLink.LinkType, linkWithType.LinkType);
                                }
                            }
                        }

                        // Remove deleted links
                        foreach (var linkedApp in linkedApplications)
                        {
                            var stillSelected = selectedLinksWithTypes.Any(selected => selected.ReferenceNumber == linkedApp.ReferenceNumber);
                            if (!stillSelected)
                            {
                                await _applicationLinksService.DeleteAsync(linkedApp.Id);
                                
                                var reverseLink = await _applicationLinksService.GetLinkedApplicationAsync(CurrentApplicationId ?? Guid.Empty, linkedApp.ApplicationId);
                                await _applicationLinksService.DeleteAsync(reverseLink.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating application links");
            }

            return new JsonResult(new { success = true });
        }

        private static ApplicationLinkType GetReverseLinkType(ApplicationLinkType linkType)
        {
            return linkType switch
            {
                ApplicationLinkType.Parent => ApplicationLinkType.Child,
                ApplicationLinkType.Child => ApplicationLinkType.Parent,
                ApplicationLinkType.Related => ApplicationLinkType.Related,
                _ => ApplicationLinkType.Related
            };
        }

        public async Task<IActionResult> OnGetApplicationDetailsByReferenceAsync(string referenceNumber)
        {
            try
            {
                var details = await _applicationLinksService.GetApplicationDetailsByReferenceAsync(referenceNumber);
                return new JsonResult(details);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting application details for reference number: {ReferenceNumber}", referenceNumber);
                return new JsonResult(new ApplicationLinksInfoDto
                {
                    ReferenceNumber = referenceNumber,
                    ApplicantName = "Error loading",
                    Category = "Error loading",
                    ApplicationStatus = "Error loading"
                });
            }
        }

        public async Task<IActionResult> OnGetValidateLinksAsync(
            [FromQuery] List<LinkValidationRequestDto> links, 
            [FromQuery] Guid currentApplicationId)
        {
            try
            {
                if (links == null || !links.Any())
                {
                    return new JsonResult(new ApplicationLinkValidationResult());
                }

                var validationRequests = await BuildValidationRequests(links);
                
                var validationResult = await _applicationLinksService.ValidateApplicationLinksAsync(
                    currentApplicationId, 
                    validationRequests);
                    
                return new JsonResult(validationResult);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating application links");
                return new JsonResult(new ApplicationLinkValidationResult { ValidationErrors = new Dictionary<string, bool>() });
            }
        }
        
        private async Task<List<ApplicationLinkValidationRequest>> BuildValidationRequests(List<LinkValidationRequestDto> links)
        {
            var validationRequests = new List<ApplicationLinkValidationRequest>();
            var allApps = await _grantApplicationAppService.GetAllApplicationsAsync();
            
            // Handle potential duplicate reference numbers
            var appLookup = allApps
                .GroupBy(a => a.ReferenceNo)
                .ToDictionary(g => g.Key, g => g.First().Id);
            
            foreach (var link in links)
            {
                if (!string.IsNullOrEmpty(link.ReferenceNumber) && appLookup.TryGetValue(link.ReferenceNumber, out var appId))
                {
                    validationRequests.Add(new ApplicationLinkValidationRequest
                    {
                        TargetApplicationId = appId,
                        ReferenceNumber = link.ReferenceNumber,
                        LinkType = link.LinkType
                    });
                }
            }
            
            return validationRequests;
        }
    }
}

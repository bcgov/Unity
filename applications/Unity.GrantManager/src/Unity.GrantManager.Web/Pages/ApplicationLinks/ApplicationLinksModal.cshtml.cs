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
                    List<ApplicationLinksInfoDto>? linkedApplications = JsonConvert.DeserializeObject<List<ApplicationLinksInfoDto>>(LinkedApplicationsList!) ?? [];

                    // Refresh from database instead of deserializing stale client data coming in to catch race conditions added.
                    var allLinks = await _applicationLinksService.GetListByApplicationAsync(CurrentApplicationId ?? Guid.Empty);
                    // Filter out the reverse links
                    var databaseLinkedApplications = allLinks.Where(item => item.ApplicationId != CurrentApplicationId).ToList();

                    // We only care if the data in the database is different to do the validation.
                    var listsAreEqual = new HashSet<ApplicationLinksInfoDto>(linkedApplications, new ApplicationLinksInfoDtoComparer()).SetEquals(databaseLinkedApplications);
                    if (!listsAreEqual)
                    {
                        var linkValidationResult = await ValidateOnPostLinks(
                            selectedLinksWithTypes ?? [],
                            grantApplications ?? [],
                            databaseLinkedApplications);

                        if (linkValidationResult.HasErrors)
                        {
                            return new JsonResult(new
                            {
                                success = false,
                                //Updates have occured while this window has been opened
                                message = string.Join(", ", linkValidationResult.ErrorMessages.Select(kvp => $"[{kvp.Key}]: {kvp.Value}"))
                        });
                        }
                    }


                    if (selectedLinksWithTypes != null && grantApplications != null && linkedApplications != null)
                    {
                        // Add new links and update existing ones
                        foreach (var linkWithType in selectedLinksWithTypes)
                        {
                            var existingLink = linkedApplications.Find(app => app.ReferenceNumber == linkWithType.ReferenceNumber);
                            if (existingLink == null)
                            {
                                await AddLink(linkWithType, grantApplications);
                            }
                            else
                            {
                                await UpdateLink(linkWithType, existingLink);
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

        /// <summary>
        /// Comparer to check for Application, LinkType and ProjectName when comparing data thats currently stored in the running
        /// window versus what is stored in the database. Used to assist with race conditions prior to submitting from the modal.
        /// </summary>
        private sealed class ApplicationLinksInfoDtoComparer : IEqualityComparer<ApplicationLinksInfoDto>
        {
            public bool Equals(ApplicationLinksInfoDto? x, ApplicationLinksInfoDto? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                return x.ApplicationId == y.ApplicationId && x.LinkType == y.LinkType && x.ProjectName == y.ProjectName;
            }

            public int GetHashCode(ApplicationLinksInfoDto obj) => obj.ApplicationId.GetHashCode();
        }

        /// <summary>
        /// If there is an inequality between what is in the application modal for links and the database, re-run the 
        /// validation checks to compare what is stored in the database rather than the local user window
        /// </summary>
        /// <param name="newLinks">Link change the user is requesting</param>
        /// <param name="currentApplications">List of applications to retrieve their reference numbers for generating links</param>
        /// <param name="existingLinks">Existing links to compare against for validation</param>
        /// <seealso cref="ApplicationLinksAppService.ValidateApplicationLinksAsync(Guid, List{ApplicationLinkValidationRequest})"/>
        /// <returns>List of ApplicationLinkValidationResult</returns>
        private async Task<ApplicationLinkValidationResult> ValidateOnPostLinks(
            List<LinkWithType> newLinks,
            List<GrantApplicationLiteDto> currentApplications,
            List<ApplicationLinksInfoDto> existingLinks)
        {
            var validateAllLinks = new List<ApplicationLinkValidationRequest>();

            validateAllLinks.AddRange([.. newLinks.Select(link =>
                new ApplicationLinkValidationRequest
                {
                    TargetApplicationId = currentApplications!.Single(app => app.ReferenceNo == link.ReferenceNumber).Id,
                    ReferenceNumber = link.ReferenceNumber,
                    LinkType = link.LinkType
                })]);

            validateAllLinks.AddRange([.. existingLinks.Select(app =>
                new ApplicationLinkValidationRequest
                {
                    TargetApplicationId = app.ApplicationId,
                    ReferenceNumber = app.ReferenceNumber,
                    LinkType = app.LinkType
                }
            )]);

            return await _applicationLinksService.ValidateApplicationLinksAsync(CurrentApplicationId ?? Guid.Empty, validateAllLinks);
        }


        private async Task AddLink(LinkWithType linkWithType, List<GrantApplicationLiteDto> grantApplications)
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


        private async Task UpdateLink(LinkWithType linkWithType, ApplicationLinksInfoDto existingLink)
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
                if (links == null || links.Count == 0)
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

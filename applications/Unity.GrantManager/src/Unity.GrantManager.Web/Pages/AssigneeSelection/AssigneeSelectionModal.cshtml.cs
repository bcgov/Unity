using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Users;
using Unity.GrantManager.Web.Models;

namespace Unity.GrantManager.Web.Pages.AssigneeSelection
{
    public class AssigneeSelectionModalModel : AbpPageModel
    {
        [BindProperty]
        [DisplayName("Owner")]
        public Guid? AssigneeId { get; set; }
        public List<SelectListItem> AssigneeList { get; set; } = new();
        public List<GrantApplicationAssigneeDto> AllAssigneeList { get; set; } = new();

        [BindProperty]
        public string CurrentAssigneeList { get; set; } = string.Empty;

        [BindProperty]
        public string CommonAssigneeList { get; set; } = string.Empty;

        [BindProperty]
        public string UnCommonAssigneeList { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedApplicationIds { get; set; } = string.Empty;

        [BindProperty]
        public string ActionType { get; set; } = string.Empty;

        [BindProperty]
        [DisplayName("Assignees")]
        public string? SelectedAssignees { get; set; } = string.Empty;

        [BindProperty]
        public string AllAssignees { get; set; } = string.Empty;

        public Guid OwnerUserId { get; set; }


        private readonly GrantApplicationAppService _applicationService;
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;        
        private readonly ApplicationIdsCacheService _cacheService;
        private readonly IApplicationAssignmentsService _applicationAssignmentsService;

        public AssigneeSelectionModalModel(GrantApplicationAppService applicationService,
            IIdentityUserIntegrationService identityUserLookupAppService,
            ApplicationIdsCacheService cacheService,
            IApplicationAssignmentsService applicationAssignmentsService)
        {
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _identityUserLookupAppService = identityUserLookupAppService ?? throw new ArgumentNullException(nameof(identityUserLookupAppService));
            _cacheService = cacheService;
            _applicationAssignmentsService = applicationAssignmentsService;
        }

        public async Task OnGetAsync(string cacheKey, string actionType)
        {
            ActionType = actionType;

            try
            {
                // Retrieve application IDs from distributed cache
                var selectedApplicationIds = await _cacheService.GetApplicationIdsAsync(cacheKey);

                if (selectedApplicationIds == null || selectedApplicationIds.Count == 0)
                {
                    // Cache expired or invalid - show error
                    Logger.LogWarning("Cache key expired or invalid: {CacheKey}", cacheKey);
                    ViewData["Error"] = "The session has expired. Please try selecting applications and try again.";
                    return;
                }

                // Store as JSON string for POST handler (still needed)
                SelectedApplicationIds = JsonConvert.SerializeObject(selectedApplicationIds);

                // Clean up cache after retrieval (one-time use)
                await _cacheService.RemoveAsync(cacheKey);

                // Load users, assignees, and applications (existing logic)
                var users = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
                PopulateAssigneeList(users);

                var assignees = await _applicationAssignmentsService.GetListWithApplicationIdsAsync(selectedApplicationIds);
                var applications = await _applicationService.GetApplicationListAsync(selectedApplicationIds);

                PopulateAssignees(users, assignees, selectedApplicationIds);
                AssignOwnerForApplications(applications);

                Logger.LogInformation("Successfully loaded assignee selection modal for {Count} applications", selectedApplicationIds.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading assignee selection modal");
                ViewData["Error"] = "An error occurred while loading the assignee selection. Please try again.";
            }
        }

        private void PopulateAssigneeList(ListResultDto<UserData> users)
        {
            AssigneeList = users.Items
                .OrderBy(s => s.UserName)
                .Select(user => new SelectListItem
                {
                    Value = user.Id.ToString(),
                    Text = $"{user.Name} {user.Surname}"
                }).ToList();

            AllAssigneeList = users.Items
                .Select(user => new GrantApplicationAssigneeDto
                {
                    Id = user.Id,
                    FullName = $"{user.Name} {user.Surname}"
                }).ToList();

            AllAssignees = JsonConvert.SerializeObject(AllAssigneeList);
        }

        private void PopulateAssignees(ListResultDto<UserData> users,
            List<GrantApplicationAssigneeDto> assignees,
            List<Guid> selectedApplicationIds)
        {
            var currentAssigneeList = assignees
                .Select(assignee => new GrantApplicationAssigneeDto
                {
                    Id = assignee.Id,
                    FullName = $"{users.Items.FirstOrDefault(s => s.Id == assignee.AssigneeId)?.Name} {users.Items.FirstOrDefault(s => s.Id == assignee.AssigneeId)?.Surname}",
                    Duty = assignee.Duty,
                    AssigneeId = assignee.AssigneeId,
                    ApplicationId = assignee.ApplicationId,
                }).ToList();

            // Categorize Assignees
            CategorizeAssignees(assignees, selectedApplicationIds, users, out var commonAssigneeList, out var unCommonAssigneeList);

            CommonAssigneeList = JsonConvert.SerializeObject(commonAssigneeList);
            UnCommonAssigneeList = JsonConvert.SerializeObject(unCommonAssigneeList);
            CurrentAssigneeList = JsonConvert.SerializeObject(currentAssigneeList);
        }

        private static void CategorizeAssignees(List<GrantApplicationAssigneeDto> assignees,
            List<Guid> selectedApplicationIds,
            ListResultDto<UserData> users,
            out List<GrantApplicationAssigneeDto> commonAssigneeList,
            out List<GrantApplicationAssigneeDto> unCommonAssigneeList)
        {
            commonAssigneeList = [];
            unCommonAssigneeList = [];

            var commonArray = assignees
                .Where(a => selectedApplicationIds.Contains(a.ApplicationId))
                .GroupBy(a => new { a.AssigneeId, a.Duty })
                .Where(group => group.Count() == selectedApplicationIds.Count)
                .Select(group => group.Key);

            // Populate common assignees
            foreach (var common in commonArray)
            {
                var user = users.Items.FirstOrDefault(s => s.Id == common.AssigneeId);
                if (user != null)
                {
                    commonAssigneeList.Add(new GrantApplicationAssigneeDto
                    {
                        Id = user.Id,
                        FullName = $"{user.Name} {user.Surname}",
                        Duty = common.Duty,
                        AssigneeId = user.Id,
                    });
                }
            }

            // Populate uncommon assignees
            var uncommonAssignees = assignees
                .Where(a => selectedApplicationIds.Contains(a.ApplicationId))
                .Where(a => !commonArray.Any(c => c.AssigneeId == a.AssigneeId && c.Duty == a.Duty))
                .GroupBy(a => new { a.AssigneeId, a.FullName, a.Duty })
                .Select(group => group.Key);

            foreach (var uncommon in uncommonAssignees)
            {
                var user = users.Items.FirstOrDefault(s => s.Id == uncommon.AssigneeId);
                if (user != null)
                {
                    unCommonAssigneeList.Add(new GrantApplicationAssigneeDto
                    {
                        Id = user.Id,
                        FullName = $"{user.Name} {user.Surname}",
                        Duty = uncommon.Duty,
                        AssigneeId = user.Id,
                    });
                }
            }
        }

        private void AssignOwnerForApplications(List<GrantApplicationDto> applications)
        {
            if (applications.Count == 1)
            {
                AssigneeId = applications[0].OwnerId ?? AssigneeId;
            }
            else
            {
                var allHaveSameOwner = applications.Select(a => a.OwnerId).Distinct().Count() == 1;
                AssigneeId = allHaveSameOwner ? applications[0].OwnerId : Guid.Empty;
                if (!allHaveSameOwner)
                {
                    AssigneeList.Add(new SelectListItem { Value = Guid.Empty.ToString(), Text = "Various Owners" });
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var applicationIds = DeserializeJson<List<Guid>>(SelectedApplicationIds);
                if (applicationIds == null) return NoContent();

                var currentAssigneeList = DeserializeJson<List<GrantApplicationAssigneeDto>>(CurrentAssigneeList) ?? [];
                var selectedAssignees = SelectedAssignees == null ? [] : DeserializeJson<List<AssigneeDuty>>(SelectedAssignees) ?? [];

                await ProcessAssigneesForApplications(applicationIds, currentAssigneeList, selectedAssignees);
                await UpdateOwnerForApplications(applicationIds);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating application status");
            }

            return NoContent();
        }

        private async Task ProcessAssigneesForApplications(List<Guid> applicationIds, List<GrantApplicationAssigneeDto> currentAssigneeList,
            List<AssigneeDuty> selectedAssignees)
        {
            if (selectedAssignees?.Count > 0)
            {
                await AddOrUpdateAssignees(applicationIds, selectedAssignees);
                await RemoveAssignees(applicationIds, currentAssigneeList, selectedAssignees, true);
            }
            else if (currentAssigneeList?.Count > 0)
            {
                await RemoveAssignees(applicationIds, currentAssigneeList, selectedAssignees ?? [], false);
            }
        }

        private async Task RemoveAssignees(List<Guid> applicationIds, List<GrantApplicationAssigneeDto> currentAssigneeList,
            List<AssigneeDuty> selectedAssignees, bool unselected)
        {
            foreach (var applicationId in applicationIds)
            {
                var currentAssigneesForApplication = currentAssigneeList?.Where(x => x.ApplicationId == applicationId).ToList();
                if (currentAssigneesForApplication == null || currentAssigneesForApplication.Count == 0) continue;

                var assigneesToRemove = unselected
                    ? currentAssigneesForApplication.Where(assignee => selectedAssignees.TrueForAll(x => x.Id != assignee.AssigneeId.ToString())).ToList()
                    : currentAssigneesForApplication;

                foreach (var assignee in assigneesToRemove)
                {
                    await _applicationAssignmentsService.DeleteAssigneeAsync(applicationId, assignee.AssigneeId);
                }
            }
        }

        private async Task AddOrUpdateAssignees(List<Guid> applicationIds, List<AssigneeDuty> selectedAssignees)
        {
            foreach (var applicationId in applicationIds)
            {
                foreach (var assignee in selectedAssignees)
                {
                    await _applicationAssignmentsService.InsertAssigneeAsync(applicationId, new Guid(assignee.Id), assignee.Duty);
                }
            }
        }

        private async Task UpdateOwnerForApplications(List<Guid> applicationIds)
        {
            if (AssigneeId == null || AssigneeId == Guid.Empty)
            {
                foreach (var applicationId in applicationIds)
                {
                    await _applicationService.DeleteOwnerAsync(applicationId);
                }
            }
            else
            {
                foreach (var applicationId in applicationIds)
                {
                    await _applicationService.InsertOwnerAsync(applicationId, AssigneeId.Value);
                }
            }
        }

        private static T? DeserializeJson<T>(string jsonString) where T : class
        {
            return string.IsNullOrEmpty(jsonString) ? null : JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}

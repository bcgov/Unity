using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;
using static Volo.Abp.Identity.Settings.IdentitySettingNames;

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

        [BindProperty]
        public string CurrentAssignees { get; set; } = string.Empty;

        public Guid OwnerUserId { get; set; }

        public class AssigneeDuty
        {
            public required string Id { get; set; }
            public string? Duty { get; set; }
        }

        


        private readonly IApplicationStatusService _statusService;
        private readonly GrantApplicationAppService _applicationService;
        private readonly IIdentityUserLookupAppService _identityUserLookupAppService;
        private readonly IApplicationAssignmentsService _applicationAssigneeService;

        public AssigneeSelectionModalModel(IApplicationStatusService statusService,
            GrantApplicationAppService applicationService,
            IIdentityUserLookupAppService identityUserLookupAppService,
            IApplicationAssignmentsService applicationAssigneeService)
        {
            _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _identityUserLookupAppService = identityUserLookupAppService ?? throw new ArgumentNullException(nameof(identityUserLookupAppService));
            _applicationAssigneeService = applicationAssigneeService;
        }

        public IEnumerable<SelectListItem> GetSelectListItems(ApplicationStatusDto[] statuses)
        {
            return statuses.Select(status => new SelectListItem
            {
                Value = status.Id.ToString(),
                Text = status.InternalStatus.ToString(),
            });
        }

        public async Task OnGetAsync(string applicationIds, string actionType)
        {
            SelectedApplicationIds = applicationIds;
            ActionType = actionType;
            AssigneeList ??= new List<SelectListItem>();
            AllAssigneeList ??= new List<GrantApplicationAssigneeDto>();
            var currentAssigneeList = new List<GrantApplicationAssigneeDto>();
            var commonAssigneeList = new List<GrantApplicationAssigneeDto>();
            var unCommonAssigneeList = new List<GrantApplicationAssigneeDto>();

            try
            {
                var users = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
                var selectedApplicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);



                foreach (var user in users.Items.OrderBy(s => s.UserName))
                {
                    AssigneeList.Add(new SelectListItem()
                    {
                        Value = user.Id.ToString(),
                        Text = $"{user.Name} {user.Surname}",

                    });
                    AllAssigneeList.Add(new GrantApplicationAssigneeDto()
                    {
                        Id = user.Id,
                        FullName = $"{user.Name} {user.Surname}",

                    });

                }
                AllAssignees = JsonConvert.SerializeObject(AllAssigneeList.ToArray());

                if (selectedApplicationIds != null)
                {
                    var assignees = await _applicationAssigneeService.GetListWithApplicationIdsAsync(selectedApplicationIds);
                    var applications = await _applicationService.GetApplicationListAsync(selectedApplicationIds);
                    foreach (var assingee in assignees)
                    {
                        var user = users.Items.FirstOrDefault(s => s.Id == assingee.AssigneeId);
                        if (user != null)
                        {

                            currentAssigneeList.Add(new GrantApplicationAssigneeDto()
                            {
                                Id = assingee.Id,
                                FullName = $"{user.Name} {user.Surname}",
                                Duty = assingee.Duty,
                                AssigneeId = assingee.AssigneeId,
                                ApplicationId = assingee.ApplicationId,
                            });
                        }
                    }


                   

                    // Step 2: Iterate through the second list and categorize Assignees
                    var commonArray = assignees
                   .Where(a => selectedApplicationIds.Contains(a.ApplicationId))
                   .GroupBy(a => new { a.AssigneeId, a.Duty })
                   .Where(group => group.Count() == selectedApplicationIds.Count)
                   .Select(group => new { AssigneeId = group.Key.AssigneeId, Duty = group.Key.Duty })
                   .ToList();
                    foreach (var common in commonArray)
                    {
                        var user = users.Items.FirstOrDefault(s => s.Id == common.AssigneeId);
                        if(user != null)
                        {
                            commonAssigneeList.Add(new GrantApplicationAssigneeDto()
                            {
                                Id = user.Id,
                                FullName = $"{user.Name} {user.Surname}",
                                Duty = common.Duty,
                                AssigneeId = user.Id,
                            });
                        }
                      
                    }
                    var uncommonAssignees = assignees
                                            .Where(a => selectedApplicationIds.Contains(a.ApplicationId))
                                            .Where(a => !commonArray.Exists(c => c.AssigneeId == a.AssigneeId && c.Duty == a.Duty))
                                            .GroupBy(a => new { a.AssigneeId, a.FullName, a.Duty })
                                            .Select(group => new { AssigneeId = group.Key.AssigneeId, FullName = group.Key.FullName, Duty = group.Key.Duty })
                                            .ToList();

                    foreach (var uncommon in uncommonAssignees)
                    {
                        var user = users.Items.FirstOrDefault(s => s.Id == uncommon.AssigneeId);
                        if (user != null)
                        {
                            unCommonAssigneeList.Add(new GrantApplicationAssigneeDto()
                            {
                                Id = user.Id,
                                FullName = $"{user.Name} {user.Surname}",
                                Duty = uncommon.Duty,
                                AssigneeId = user.Id,
                            });
                        }

                    }

                
                    

                    if (selectedApplicationIds.Count == 1)
                    {
                        var owner = applications[0].OwnerId;
                        if (owner != null)
                        {
                            AssigneeId = owner;
                        }



                    }
                    else
                    {
                        bool allHaveSameValue = applications.Select(item => item.OwnerId).Distinct().Count() == 1;
                        if (allHaveSameValue)
                        {
                            Guid? commonValue = applications[0].OwnerId;
                            if (commonValue != null)
                            {
                                AssigneeId = commonValue;
                            }

                        }
                        else
                        {
                            AssigneeList.Add(new SelectListItem()
                            {
                                Value = Guid.Empty.ToString(),
                                Text = "Various Owners",

                            });
                            AssigneeId = Guid.Empty;

                        }

                    }

                    CommonAssigneeList = JsonConvert.SerializeObject(commonAssigneeList);
                    UnCommonAssigneeList = JsonConvert.SerializeObject(unCommonAssigneeList);
                    CurrentAssigneeList = JsonConvert.SerializeObject(currentAssigneeList);
                   
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error loading users select list");
            }

        }



        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var uncommonId = "uncommonAssignees";
                var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
                if (applicationIds != null)
                {
                    var currentAssigneeList = JsonConvert.DeserializeObject<List<GrantApplicationAssigneeDto>>(CurrentAssigneeList);
                    if (SelectedAssignees != null)
                    {
                        var selectedAssignees = JsonConvert.DeserializeObject<List<AssigneeDuty>>(SelectedAssignees);
                        if (selectedAssignees != null && selectedAssignees.Count > 0)
                        {
                            var elementToRemove = selectedAssignees.Find(e => e.Id.ToString() == uncommonId);
                            if (elementToRemove != null)
                            {
                                selectedAssignees.Remove(elementToRemove);
                                if (selectedAssignees.Count > 0)
                                {
                                    foreach (var applicationId in applicationIds)
                                    {
                                        foreach (var assignee in selectedAssignees)
                                        {
                                            await _applicationService.InsertAssigneeAsync(applicationId, new Guid(assignee.Id), assignee.Duty);

                                        }

                                    }
                                }
                                else
                                {
                                    var uncommonAssignees = JsonConvert.DeserializeObject<List<AssigneeDuty>>(UnCommonAssigneeList);
                                    if (uncommonAssignees != null)
                                    {

                               
                                    foreach (var applicationId in applicationIds)
                                    {
                                        if (currentAssigneeList != null && currentAssigneeList.Count > 0)
                                        {
                                            var currentAssigneeListSelectedApplication = currentAssigneeList.FindAll(x => x.ApplicationId == applicationId);
                                            if (currentAssigneeListSelectedApplication != null && currentAssigneeListSelectedApplication.Count > 0)
                                            {

                                                    foreach (var assignee in currentAssigneeListSelectedApplication)
                                                    {
                                                        var assigneeDetails = uncommonAssignees.Find(x => x.Id == assignee.AssigneeId.ToString());
                                                        if (assigneeDetails == null)
                                                        {
                                                            await _applicationService.DeleteAssigneeAsync(applicationId, assignee.AssigneeId);
                                                        }
                                                    };
                                              
                                            }
                                        }
                                    }
                                }
                                }

                              
                               



                            }
                            else
                            {
                                if (selectedAssignees.Count > 0)
                                {
                                    foreach (var applicationId in applicationIds)
                                    {
                                        foreach (var assignee in selectedAssignees)
                                        {
                                            await _applicationService.InsertAssigneeAsync(applicationId, new Guid(assignee.Id), assignee.Duty);

                                        }
                                        if (currentAssigneeList != null && currentAssigneeList.Count > 0)
                                        {
                                            var currentAssigneeListSelectedApplication = currentAssigneeList.FindAll(x => x.ApplicationId == applicationId);
                                            if (currentAssigneeListSelectedApplication != null && currentAssigneeListSelectedApplication.Count > 0)
                                            {
                                               
                                                foreach(var assignee in currentAssigneeListSelectedApplication)
                                                {
                                                    var assigneeDetails = selectedAssignees.Find(x => x.Id == assignee.AssigneeId.ToString());
                                                    if (assigneeDetails == null)
                                                    {
                                                        await _applicationService.DeleteAssigneeAsync(applicationId, assignee.AssigneeId);
                                                    }


                                                };
                                            }
                                        }


                                    }
                                }

                            }



                        }
                        else
                        {
                            if (currentAssigneeList != null && currentAssigneeList.Count > 0)
                            {
                                foreach (var applicationId in applicationIds)
                                {
                                    var currentAssigneeListSelectedApplication = currentAssigneeList.FindAll(x => x.ApplicationId == applicationId);
                                    if (currentAssigneeListSelectedApplication != null && currentAssigneeListSelectedApplication.Count > 0)
                                    {
                                        foreach (var assignee in currentAssigneeListSelectedApplication)
                                        {

                                            await _applicationService.DeleteAssigneeAsync(applicationId, assignee.AssigneeId);

                                        };
                                    }


                                }
                            }
                        }
                           
                    }
                    if (AssigneeId != Guid.Empty)
                    {
                        if (AssigneeId != null)
                        {
                            foreach (var applicationId in applicationIds)
                            {

                                await _applicationService.InsertOwnerAsync(applicationId, AssigneeId);

                            }
                        }
                        else
                        {

                            foreach (var applicationId in applicationIds)
                            {

                                await _applicationService.DeleteOwnerAsync(applicationId);

                            }
                        }
                    }



                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, message: "Error updating application status");
            }

            return NoContent();
        }
    }
}

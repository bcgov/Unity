using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Dashboard;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Intakes;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Web.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly IIntakeRepository _intakeRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicationTagsRepository _applicationTagsRepository; 
        private readonly IApplicationAssignmentRepository _applicationAssignmentRepository;
        private readonly IPersonRepository _personRepository;

        public List<SelectListItem> IntakeOptionsList { get; set; } = [];
        public List<SelectListItem> CategoryOptionsList { get; set; } = [];
        public List<SelectListItem> StatusOptionsList { get; set; } = [];
        public List<SelectListItem> SubStatusActionList { get; set; } = [];
        public List<SelectListItem> TagsOptionsList { get; set; } = [];
        public List<SelectListItem> AssigneesOptionList { get; set; } = [];

        [BindProperty]
        [Display(Name = "")]
        public Guid[] IntakeIds { get; set; } = [];
        [BindProperty]
        [Display(Name = "")]
        public string[]? CategoryNames { get; set; }
        [BindProperty]
        [Display(Name = "")]
        public string[]? Statuses { get; set; }
        [BindProperty]
        [Display(Name = "")]
        public string[]? SubStatuses { get; set; }

        [BindProperty]
        [Display(Name = "")]
        public DateTime? SubmissionDateFrom { get; set; }
        [BindProperty]
        [Display(Name = "")]
        public DateTime? SubmissionDateTo { get; set; }
        [BindProperty]
        [Display(Name = "")]
        public string[]? Tags { get; set; }
        [BindProperty]
        [Display(Name = "")]
        public string[]? Assignees { get; set; }

        public List<DashboardIntakeDto> DashboardIntakes { get; set; } = [];

        public IndexModel(IIntakeRepository intakeRepository,
            IApplicationFormRepository applicationFormRepository,
            IApplicationStatusRepository applicationStatusRepository,
            IApplicationTagsRepository applicationTagsRepository,
            IApplicationAssignmentRepository applicationAssignmentRepository,
            IPersonRepository personRepository)
        {
            _intakeRepository = intakeRepository;
            _applicationFormRepository = applicationFormRepository;
            _applicationStatusRepository = applicationStatusRepository;
            _applicationTagsRepository = applicationTagsRepository;
            _applicationAssignmentRepository = applicationAssignmentRepository;
            _personRepository = personRepository;
        }

        public async Task OnGetAsync()
        {
            using (_intakeRepository.DisableTracking())
            using (_applicationFormRepository.DisableTracking())
            {
                var intakesQ = from intakesq in await _intakeRepository.GetQueryableAsync()
                               join formsq in await _applicationFormRepository.GetQueryableAsync() on intakesq.Id equals formsq.IntakeId
                               select new IntakeQ
                               {
                                   IntakeId = intakesq.Id,
                                   IntakeCreationTime = intakesq.CreationTime,
                                   IntakeName = intakesq.IntakeName,
                                   Category = formsq.Category ?? DashboardConsts.EmptyValue
                               };

                var intakeR = await intakesQ.ToListAsync();
                if (intakeR.Count == 0) return;

                IntakeOptionsList = intakeR.DistinctBy(s => s.IntakeId).Select(intake => new SelectListItem { Value = intake.IntakeId.ToString(), Text = intake.IntakeName }).ToList();
                var latestIntakeId = intakeR.OrderByDescending(intake => intake.IntakeCreationTime).FirstOrDefault()?.IntakeId;
                IntakeIds = [latestIntakeId ?? Guid.Empty];

                foreach (var intake in intakeR)
                {
                    List<string> categoryList = [.. intakeR.Where(s => !string.IsNullOrWhiteSpace(s.Category) && s.IntakeId == intake.IntakeId)
                        .Select(s => s.Category)
                        .Distinct()
                        .OrderBy(c => c)];

                    DashboardIntakes.Add(new()
                    {
                        IntakeId = intake.IntakeId,
                        IntakeName = intake.IntakeName,
                        Categories = categoryList
                    });

                    if (intake.IntakeId == latestIntakeId)
                    {
                        CategoryOptionsList = categoryList.Select(category => new SelectListItem { Value = category, Text = category }).ToList();
                        CategoryNames = [.. categoryList];
                    }
                }

                var statuses = await _applicationStatusRepository.GetListAsync();
                StatusOptionsList = statuses.Select(s => new SelectListItem { Value = s.StatusCode.ToString(), Text = s.InternalStatus }).ToList();
                Statuses = statuses.Select(s => s.StatusCode.ToString()).ToArray();
                SubStatusActionList = AssessmentResultsOptionsList.SubStatusActionList.Select(s => new SelectListItem { Value = s.Key, Text = s.Value }).ToList();
                SubStatusActionList.Add(new SelectListItem { Value = DashboardConsts.EmptyValue, Text = DashboardConsts.EmptyValue }); //for applications with no Sub-Status
                SubStatuses = SubStatusActionList.Select(s => s.Value).ToArray();

                await GetTagsFilter();
                await GetUsersFilter();
            }
        }

        private async Task GetUsersFilter()
        {
            var userAssignments = await _applicationAssignmentRepository.GetQueryableAsync();
            var users = await _personRepository.GetQueryableAsync();

            var assignees = from userAssignment in userAssignments
                            join user in users on userAssignment.AssigneeId equals user.Id
                            select new { user.Id, user.FullName };

            var distinctOrderedUsers = assignees.Distinct().OrderBy(s => s.FullName);

            AssigneesOptionList = distinctOrderedUsers.Select(user => new SelectListItem
            {
                Value = user.Id.ToString(),
                Text = !string.IsNullOrEmpty(user.FullName) ? user.FullName : DashboardConsts.EmptyValue,
            }).ToList();

            AssigneesOptionList.Add(new SelectListItem { Value = string.Empty, Text = DashboardConsts.EmptyValue });
            Assignees = AssigneesOptionList.Select(s => s.Value).Distinct().ToArray();
        }

        private async Task GetTagsFilter()
        {
            var tagResult = await _applicationTagsRepository.GetListAsync();
            var tags = tagResult.SelectMany(tag => tag.Text.Split(',').Select(t => t.Trim())).Distinct();

            TagsOptionsList = tags.Select(tag => new SelectListItem
            {
                Value = tag,
                Text = !string.IsNullOrEmpty(tag) ? tag : DashboardConsts.EmptyValue
            }).ToList();

            TagsOptionsList.Add(new SelectListItem { Value = string.Empty, Text = DashboardConsts.EmptyValue });
            Tags = TagsOptionsList.Select(tag => tag.Value).Distinct().ToArray();
        }

        private sealed class IntakeQ
        {
            public Guid IntakeId { get; internal set; }
            public string? Category { get; internal set; } = null;
            public DateTime IntakeCreationTime { get; internal set; }
            public string IntakeName { get; internal set; } = string.Empty;
        }
    }
}

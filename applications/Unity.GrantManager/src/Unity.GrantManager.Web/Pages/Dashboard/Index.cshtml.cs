using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Applications;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Dashboard;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Web.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly IIntakeRepository _intakeRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        public List<SelectListItem> IntakeOptionsList { get; set; } = [];
        public List<SelectListItem> CategoryOptionsList { get; set; } = [];
        public List<SelectListItem> StatusOptionsList { get; set; } = [];
        public List<SelectListItem> SubStatusActionList { get; set; } = [];

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
        public List<DashboardIntakeDto> DashboardIntakes { get; set; } = [];

        public IndexModel(IIntakeRepository intakeRepository, IApplicationFormRepository applicationFormRepository, IApplicationStatusRepository applicationStatusRepository)
        {
            _intakeRepository = intakeRepository;
            _applicationFormRepository = applicationFormRepository;
            _applicationStatusRepository = applicationStatusRepository;
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
                        .Distinct()
                        .OrderBy(c => c.Category).Select(s => s.Category)];

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
            }
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

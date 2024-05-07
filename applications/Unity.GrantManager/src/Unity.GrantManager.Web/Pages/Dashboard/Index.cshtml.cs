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
            List<GrantManager.Intakes.Intake> intakes = await _intakeRepository.GetListAsync();
            IntakeOptionsList = intakes.Select(intake => new SelectListItem { Value = intake.Id.ToString(), Text = intake.IntakeName }).ToList();
            GrantManager.Intakes.Intake? latestIntake = intakes.OrderByDescending(intake => intake.CreationTime).FirstOrDefault();
            IntakeIds = [latestIntake?.Id ?? Guid.Empty];

            foreach (var intake in intakes)
            {
                var query = from appForm in _applicationFormRepository.GetQueryableAsync().GetAwaiter().GetResult()
                            where appForm.IntakeId == intake.Id
                            select appForm.Category == null ? DashboardConsts.EmptyValue : appForm.Category;

                List<string> categoryList = [.. query.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(c => c)];
                DashboardIntakeDto dto = new() { IntakeId = intake.Id, IntakeName = intake.IntakeName, Categories = categoryList };
                DashboardIntakes.Add(dto);
                if (intake.Id == latestIntake?.Id)
                {
                    CategoryOptionsList = categoryList.Select(category => new SelectListItem { Value = category, Text = category }).ToList();
                    CategoryNames = [.. categoryList];
                }
            }
            var statuses = await _applicationStatusRepository.GetListAsync();
            StatusOptionsList = statuses.Select(s => new SelectListItem { Value = s.StatusCode.ToString(), Text = s.InternalStatus }).ToList();
            Statuses = statuses.Select(s => s.StatusCode.ToString()).ToArray();
            SubStatusActionList = AssessmentResultsOptionsList.SubStatusActionList.Select(s=> new SelectListItem { Value = s.Key, Text = s.Value }).ToList();
            SubStatusActionList.Add(new SelectListItem { Value = DashboardConsts.EmptyValue, Text = DashboardConsts.EmptyValue }); //for applications with no Sub-Status
            SubStatuses = SubStatusActionList.Select(s => s.Value).ToArray();
        }
    }
}

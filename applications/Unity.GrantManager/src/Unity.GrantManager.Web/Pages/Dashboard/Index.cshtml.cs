using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Applications;
using System.Linq.Dynamic.Core;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.Repositories;



namespace Unity.GrantManager.Web.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly IIntakeRepository _intakeRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;
        public List<SelectListItem> IntakeOptionsList { get; set; }
        public List<SelectListItem> CategoryOptionsList { get; set; } = [];
        
        [BindProperty]
        [Display(Name = "Dashboard:IntakeId")]
        public Guid IntakeId { get; set; }
        [BindProperty]
        [Display(Name = "Dashboard:CategoryName")]
        public string? CategoryName { get; set; }
        public List<DashboardIntakeDto> DashboardIntakes { get; set; } = [];
#pragma warning disable CS8618
        public IndexModel(IIntakeRepository intakeRepository, IApplicationFormRepository applicationFormRepository)
#pragma warning restore CS8618
        {
            _intakeRepository = intakeRepository;
            _applicationFormRepository = applicationFormRepository;
        }
        
        public void OnGet()
        {
            List<GrantManager.Intakes.Intake> intakes = _intakeRepository.GetListAsync().Result;
            IntakeOptionsList = intakes.Select(intake => new SelectListItem { Value = intake.Id.ToString(), Text = intake.IntakeName }).ToList();
            GrantManager.Intakes.Intake? latestIntake = intakes.OrderByDescending(intake => intake.CreationTime).FirstOrDefault();
            IntakeId = latestIntake?.Id ?? Guid.Empty;
            foreach (var intake in intakes)
            {
                var query = from appForm in _applicationFormRepository.GetQueryableAsync().GetAwaiter().GetResult()
                            where appForm.IntakeId == intake.Id
                            select appForm.Category == null ? "None" : appForm.Category;
                List<string> categoryList = [.. query.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(c => c)];
                DashboardIntakeDto dto = new() { IntakeId = intake.Id, IntakeName = intake.IntakeName, Categories = categoryList };
                DashboardIntakes.Add(dto);
                if (intake.Id == latestIntake?.Id)
                {
                    CategoryOptionsList = categoryList.Select(category => new SelectListItem { Value = category, Text = category }).ToList();
                }
            }
        }
    }
}

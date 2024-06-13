using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;
using Microsoft.Extensions.Configuration;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Features;
using System.Linq;
using Unity.GrantManager.Flex;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class DetailsModel : AbpPageModel
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;
        private readonly IWorksheetListAppService _worksheetListAppService;
        private readonly IFeatureChecker _featureChecker;

        [BindProperty(SupportsGet = true)]
        public string? SubmissionId { get; set; } = null;
        public string? SelectedAction { get; set; } = null;
        public IFormFile? Attachment { get; set; } = default;
        public List<SelectListItem> ActionList { get; set; } =
        [
            new() { Value = "true", Text = "Recommended for Approval"},
            new() { Value = "false", Text = "Recommended for Denial"}
        ];

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ApplicationFormId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid AssessmentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ApplicationFormSubmissionId { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public string? ChefsSubmissionId { get; set; } = null;
        
        [BindProperty(SupportsGet = true)]
        public string? ApplicationFormSubmissionData { get; set; } = null;

        [BindProperty(SupportsGet = true)]

        public string? ApplicationFormSubmissionHtml { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public bool? HasRenderedHTML { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public Guid? CurrentUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CurrentUserName { get; set; }
        public string Extensions { get; set; }
        public string MaxFileSize { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<WorksheetBasicDto> CustomTabs { get; set; } = [];

        public DetailsModel(GrantApplicationAppService grantApplicationAppService,
            IWorksheetListAppService worksheetListAppService,
            IFeatureChecker featureChecker,
            ICurrentUser currentUser,
            IConfiguration configuration)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _worksheetListAppService = worksheetListAppService;
            _featureChecker = featureChecker;
            CurrentUserId = currentUser.Id;
            CurrentUserName = currentUser.SurName + ", " + currentUser.Name;
            Extensions = configuration["S3:DisallowedFileTypes"] ?? "";
            MaxFileSize = configuration["S3:MaxFileSize"] ?? "";
        }

        public async Task OnGetAsync()
        {
            ApplicationFormSubmission applicationFormSubmission = await _grantApplicationAppService.GetFormSubmissionByApplicationId(ApplicationId);

            if (await _featureChecker.IsEnabledAsync("Unity.Flex"))
            {
                var worksheets = await _worksheetListAppService.GetListByCorrelationAsync(applicationFormSubmission.ApplicationFormId, CorrelationConsts.Form);
                CustomTabs = worksheets.Where(s => !FlexConsts.UiAnchors.Contains(s.UiAnchor)).ToList();
            }

            if (applicationFormSubmission != null)
            {
                ApplicationFormId = applicationFormSubmission.ApplicationFormId;
                ChefsSubmissionId = applicationFormSubmission.ChefsSubmissionGuid;
                ApplicationFormSubmissionId = applicationFormSubmission.Id.ToString();
                ApplicationFormSubmissionData = applicationFormSubmission.Submission;
                ApplicationFormSubmissionHtml = applicationFormSubmission.RenderedHTML;
                HasRenderedHTML = !string.IsNullOrEmpty(applicationFormSubmission.RenderedHTML);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await Task.CompletedTask;
            return Page();
        }
    }
}

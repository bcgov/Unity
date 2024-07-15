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
using Unity.Flex.WorksheetLinks;
using Newtonsoft.Json.Linq;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class DetailsModel : AbpPageModel
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;
        private readonly IWorksheetLinkAppService _worksheetLinkAppService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
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
        public Guid ApplicationFormVersionId { get; set; }

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
        public List<BoundWorksheet> CustomTabs { get; set; } = [];

        public DetailsModel(GrantApplicationAppService grantApplicationAppService,
            IWorksheetLinkAppService worksheetLinkAppService,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IFeatureChecker featureChecker,
            ICurrentUser currentUser,
            IConfiguration configuration)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _worksheetLinkAppService = worksheetLinkAppService;
            _featureChecker = featureChecker;
            _applicationFormVersionAppService = applicationFormVersionAppService;
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
                // Need to look at finding another way to extract / store this info on intake
                JObject submission = JObject.Parse(applicationFormSubmission.Submission);
                JToken? tokenFormVersionId = submission.SelectToken("submission.formVersionId");
                var sformVersionId = Guid.Parse(tokenFormVersionId?.Value<string>() ?? Guid.Empty.ToString());                

                var formVersion = await _applicationFormVersionAppService.GetByChefsFormVersionId(sformVersionId);
                ApplicationFormVersionId = formVersion?.Id ?? Guid.Empty;
                var worksheetLinks = await _worksheetLinkAppService.GetListByCorrelationAsync(ApplicationFormVersionId, CorrelationConsts.FormVersion);
                var tabs = worksheetLinks.Where(s => !FlexConsts.UiAnchors.Contains(s.UiAnchor)).Select(s => new { worksheet = s.Worksheet, uiAnchor = s.UiAnchor }).ToList();

                foreach (var tab in tabs)
                {
                    CustomTabs.Add(new BoundWorksheet() { Worksheet = tab.worksheet, UiAnchor = tab.uiAnchor });
                }
            }

            if (applicationFormSubmission != null)
            {
                ApplicationFormId = applicationFormSubmission.ApplicationFormId;
                ChefsSubmissionId = applicationFormSubmission.ChefsSubmissionGuid;
                ApplicationFormSubmissionId = applicationFormSubmission.Id.ToString();
                HasRenderedHTML = !string.IsNullOrEmpty(applicationFormSubmission.RenderedHTML);
                if (!string.IsNullOrEmpty(applicationFormSubmission.RenderedHTML))
                {
                    ApplicationFormSubmissionHtml = applicationFormSubmission.RenderedHTML;
                }
                else
                {
                    ApplicationFormSubmissionData = applicationFormSubmission.Submission;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await Task.CompletedTask;
            return Page();
        }
    }

    public class BoundWorksheet
    {
        public WorksheetBasicDto? Worksheet { get; set; }
        public string UiAnchor { get; set; } = string.Empty;
    }
}

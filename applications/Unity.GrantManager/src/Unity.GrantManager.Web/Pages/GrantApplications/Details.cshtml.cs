using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using Unity.Flex.WorksheetLinks;
using Unity.Flex.Worksheets;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.GrantManager.GrantApplications;
using Unity.AI.Web.PromptTools;
using Unity.GrantManager.Zones;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class DetailsModel : AbpPageModel
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;
        private readonly IWorksheetLinkAppService _worksheetLinkAppService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IScoresheetRepository _scoresheetRepository;
        private readonly IFeatureChecker _featureChecker;
        protected readonly IZoneManagementAppService _zoneManagementAppService;

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
        public string? ApplicationFormSchema { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public string? ApplicationFormSubmissionHtml { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public bool? HasRenderedHTML { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public bool RenderFormIoToHtml { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public Guid? CurrentUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CurrentUserName { get; set; }
        public string Extensions { get; set; }
        public string MaxFileSize { get; set; }
        public string EmailAttachmentMaxFileSize { get; set; }
        public string TotalEmailAttachmentMaxFileSize { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<BoundWorksheet> CustomTabs { get; set; } = [];

        [BindProperty]
        public HashSet<string> ZoneStateSet { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public bool IsDevPromptControlsEnabled { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DefaultPromptVersion { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ApplicationScoresheetSchemaJson { get; set; }

        public DetailsModel(
            GrantApplicationAppService grantApplicationAppService,
            IWorksheetLinkAppService worksheetLinkAppService,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IScoresheetRepository scoresheetRepository,
            IFeatureChecker featureChecker,
            ICurrentUser currentUser,
            IConfiguration configuration,
            IAIPromptToolViewOptionsProvider aiPromptToolViewOptionsProvider,
            IZoneManagementAppService zoneManagementAppService)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _worksheetLinkAppService = worksheetLinkAppService;
            _featureChecker = featureChecker;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _scoresheetRepository = scoresheetRepository;
            _zoneManagementAppService = zoneManagementAppService;

            CurrentUserId = currentUser.Id;
            CurrentUserName = currentUser.SurName + ", " + currentUser.Name;
            Extensions = configuration["S3:DisallowedFileTypes"] ?? "";
            MaxFileSize = configuration["S3:MaxFileSize"] ?? "";
            EmailAttachmentMaxFileSize = configuration["S3:EmailAttachmentMaxFileSize"] ?? "20";
            TotalEmailAttachmentMaxFileSize = configuration["S3:EmailAttachmentsTotalMaxFileSize"] ?? "25";
            IsDevPromptControlsEnabled = aiPromptToolViewOptionsProvider.IsDevPromptControlsEnabled;
            DefaultPromptVersion = aiPromptToolViewOptionsProvider.DefaultPromptVersion;
        }

        public async Task OnGetAsync()
        {
            ApplicationFormSubmission applicationFormSubmission = await _grantApplicationAppService.GetFormSubmissionByApplicationId(ApplicationId);
            ZoneStateSet = await _zoneManagementAppService.GetZoneStateSetAsync(applicationFormSubmission.ApplicationFormId);
            
            var formVersion = applicationFormSubmission.ApplicationFormVersionId.HasValue
                ? await _applicationFormVersionAppService.GetAsync(applicationFormSubmission.ApplicationFormVersionId.Value)
                : null;
            ApplicationFormSchema = formVersion?.FormSchema ?? string.Empty;
            ApplicationFormVersionId = formVersion?.Id ?? Guid.Empty;

            if (await _featureChecker.IsEnabledAsync("Unity.Flex"))
            {
                var worksheetLinks = await _worksheetLinkAppService.GetListByCorrelationAsync(ApplicationFormVersionId, CorrelationConsts.FormVersion);
                var tabs = worksheetLinks.Where(s => !FlexConsts.UiAnchors.Contains(s.UiAnchor)).Select(s => new { worksheet = s.Worksheet, uiAnchor = s.UiAnchor, order = s.Order }).ToList();

                foreach (var tab in tabs.OrderBy(s => s.order))
                {
                    CustomTabs.Add(new BoundWorksheet()
                    {
                        Worksheet = tab.worksheet,
                        UiAnchor = tab.uiAnchor,
                        Order = tab.order
                    });
                }
            }

            ApplicationFormId = applicationFormSubmission.ApplicationFormId;
            ChefsSubmissionId = applicationFormSubmission.ChefsSubmissionGuid;
            ApplicationFormSubmissionId = applicationFormSubmission.Id.ToString();
            HasRenderedHTML = !string.IsNullOrEmpty(applicationFormSubmission.RenderedHTML);
            ApplicationForm? applicationForm = await _grantApplicationAppService.GetApplicationFormAsync(ApplicationFormId);
            ArgumentNullException.ThrowIfNull(applicationForm);
            ApplicationScoresheetSchemaJson = await GetApplicationScoresheetSchemaJsonAsync(applicationForm);
            RenderFormIoToHtml = applicationForm.RenderFormIoToHtml;
            ApplicationFormSubmissionData = applicationFormSubmission.Submission;
            if (!string.IsNullOrEmpty(applicationFormSubmission.RenderedHTML) && RenderFormIoToHtml)
            {
                ApplicationFormSubmissionHtml = applicationFormSubmission.RenderedHTML;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await Task.CompletedTask;
            return Page();
        }

        private async Task<string?> GetApplicationScoresheetSchemaJsonAsync(ApplicationForm applicationForm)
        {
            if (applicationForm.ScoresheetId == null || !await _featureChecker.IsEnabledAsync("Unity.Flex"))
            {
                return null;
            }

            var scoresheet = await _scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
            if (scoresheet == null)
            {
                return null;
            }

            var scoresheetDto = ObjectMapper.Map<Unity.Flex.Domain.Scoresheets.Scoresheet, ScoresheetDto?>(scoresheet);
            return scoresheetDto == null ? null : JsonSerializer.Serialize(scoresheetDto);
        }
        
    }

    public class BoundWorksheet
    {
        public WorksheetBasicDto? Worksheet { get; set; }
        public string UiAnchor { get; set; } = string.Empty;
        public uint? Order { get; set; } = 0;
    }
}

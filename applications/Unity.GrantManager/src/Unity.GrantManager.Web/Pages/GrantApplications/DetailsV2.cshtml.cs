using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Flex.WorksheetLinks;
using Unity.Flex.Worksheets;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Zones;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class DetailsV2Model : AbpPageModel
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;
        private readonly IWorksheetLinkAppService _worksheetLinkAppService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
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

        [BindProperty(SupportsGet = true)]
        public List<BoundWorksheetV2> CustomTabs { get; set; } = [];

        [BindProperty]
        public HashSet<string> ZoneStateSet { get; set; } = [];
        public DetailsV2Model(
            GrantApplicationAppService grantApplicationAppService,
            IWorksheetLinkAppService worksheetLinkAppService,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IFeatureChecker featureChecker,
            ICurrentUser currentUser,
            IConfiguration configuration,
            IZoneManagementAppService zoneManagementAppService)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _worksheetLinkAppService = worksheetLinkAppService;
            _featureChecker = featureChecker;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _zoneManagementAppService = zoneManagementAppService;

            CurrentUserId = currentUser.Id;
            CurrentUserName = currentUser.SurName + ", " + currentUser.Name;
            Extensions = configuration["S3:DisallowedFileTypes"] ?? "";
            MaxFileSize = configuration["S3:MaxFileSize"] ?? "";
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
                    CustomTabs.Add(new BoundWorksheetV2()
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
            RenderFormIoToHtml = applicationForm.RenderFormIoToHtml;
            if (!string.IsNullOrEmpty(applicationFormSubmission.RenderedHTML) && RenderFormIoToHtml)
            {
                ApplicationFormSubmissionHtml = applicationFormSubmission.RenderedHTML;
            }
            else
            {
                ApplicationFormSubmissionData = applicationFormSubmission.Submission;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await Task.CompletedTask;
            return Page();
        }

        public async Task<IActionResult> OnGetLoadComponentAsync(string component, string tab, Guid applicationId, Guid applicationFormVersionId)
        {
            try
            {
                IHtmlContent? htmlContent = null;

                // Validate input parameters
                if (applicationId == Guid.Empty)
                {
                    return new JsonResult(new { success = false, error = "Invalid applicationId" });
                }

                switch (component)
                {
                    case "AssessmentResults":
                        htmlContent = await InvokeViewComponentDirectly("AssessmentResults", new { applicationId, applicationFormVersionId });
                        break;

                    case "ReviewList":
                        htmlContent = await InvokeViewComponentDirectly("ReviewList", new { applicationId });
                        break;

                    case "ProjectInfo":
                        try
                        {
                            htmlContent = await InvokeViewComponentDirectly("ProjectInfo", new { applicationId, applicationFormVersionId });
                        }
                        catch (Exception projEx)
                        {
                            return new JsonResult(new
                            {
                                success = false,
                                error = $"ProjectInfo component error: {projEx.Message}",
                                innerException = projEx.InnerException?.Message,
                                stackTrace = projEx.StackTrace
                            });
                        }
                        break;

                    case "ApplicantInfo":
                        htmlContent = await InvokeViewComponentDirectly("ApplicantInfo", new { applicationId, applicationFormVersionId });
                        break;

                    case "FundingAgreementInfo":
                        htmlContent = await InvokeViewComponentDirectly("FundingAgreementInfo", new { applicationId, applicationFormVersionId });
                        break;

                    case "PaymentInfo":
                        htmlContent = await InvokeViewComponentDirectly("PaymentInfo", new { applicationId, applicationFormVersionId });
                        break;

                    default:
                        return new JsonResult(new { success = false, error = "Unknown component" });
                }

                var htmlString = htmlContent != null ? GetHtmlString(htmlContent) : "";
                
                return new JsonResult(new
                {
                    success = true,
                    html = htmlString,
                    component = component,
                    tab = tab
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    component = component,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace // Add for debugging
                });
            }
        }

        private async Task<IHtmlContent> InvokeViewComponentDirectly(string componentName, object arguments)
        {
            // Use a simpler approach with StringWriter to capture the output
            using var writer = new StringWriter();

            // Create ActionContext
            var actionContext = new ActionContext(HttpContext, RouteData, PageContext.ActionDescriptor);

            // Create ViewContext with our StringWriter
            var viewContext = new ViewContext(
                actionContext,
                new FakeView(),
                new ViewDataDictionary(ViewData),
                TempData,
                writer,
                new HtmlHelperOptions()
            );

            // Get ViewComponent helper and contextualize it properly
            var viewComponentHelper = HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();

            // Cast to the correct type and contextualize
            if (viewComponentHelper is IViewContextAware contextAware)
            {
                contextAware.Contextualize(viewContext);
            }

            // Invoke the ViewComponent
            var result = await viewComponentHelper.InvokeAsync(componentName, arguments);

            return new HtmlString(GetHtmlString(result));
        }

        private static string GetHtmlString(IHtmlContent htmlContent)
        {
            using var writer = new StringWriter();
            htmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            return writer.ToString();
        }   
    }
    

    public class BoundWorksheetV2
        {
            public WorksheetBasicDto? Worksheet { get; set; }
            public string UiAnchor { get; set; } = string.Empty;
            public uint? Order { get; set; } = 0;
        }

    // Simple view implementation for ViewContext
    public class FakeView : IView
    {
        public string Path => string.Empty;

        public Task RenderAsync(ViewContext context)
        {
            return Task.CompletedTask;
        }
    }
}

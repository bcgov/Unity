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
using Unity.GrantManager.Web.Views.Shared.Components.CustomTabWidget;
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
                // Extract and validate parameters
                var componentParams = ExtractComponentParameters();
                var validationResult = ValidateParameters(applicationId, component, componentParams);
                if (validationResult != null) return validationResult;

                // Load the component
                var htmlContent = await LoadComponentAsync(component, applicationId, applicationFormVersionId, componentParams);
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
                return CreateErrorResponse(ex, component);
            }
        }

        #region Private Helper Methods

        private ComponentParameters ExtractComponentParameters()
        {
            return new ComponentParameters
            {
                InstanceCorrelationId = Guid.TryParse(Request.Query["instanceCorrelationId"].FirstOrDefault(), out var instanceId) ? instanceId : Guid.Empty,
                InstanceCorrelationProvider = Request.Query["instanceCorrelationProvider"].FirstOrDefault() ?? "",
                SheetCorrelationId = Request.Query["sheetCorrelationId"].FirstOrDefault() ?? "",
                SheetCorrelationProvider = Request.Query["sheetCorrelationProvider"].FirstOrDefault() ?? "",
                UiAnchor = Request.Query["uiAnchor"].FirstOrDefault() ?? "",
                Name = Request.Query["name"].FirstOrDefault() ?? "",
                Title = Request.Query["title"].FirstOrDefault() ?? "",
                WorksheetIdStr = Request.Query["worksheetId"].FirstOrDefault() ?? ""
            };
        }

        private static JsonResult? ValidateParameters(Guid applicationId, string component, ComponentParameters parameters)
        {
            if (applicationId == Guid.Empty)
            {
                return new JsonResult(new { success = false, error = "Invalid applicationId" });
            }

            // Special validation for CustomTabWidget
            if (component == "CustomTabWidget" && !Guid.TryParse(parameters.WorksheetIdStr, out _))
            {
                return new JsonResult(new { success = false, error = "Invalid worksheetId parameter" });
            }

            return null;
        }

        private async Task<IHtmlContent?> LoadComponentAsync(string component, Guid applicationId, Guid applicationFormVersionId, ComponentParameters parameters)
        {
            return component switch
            {
                "AssessmentResults" => await InvokeViewComponentDirectly("AssessmentResults", new { applicationId, applicationFormVersionId }),
                "ReviewList" => await InvokeViewComponentDirectly("ReviewList", new { applicationId }),
                "ProjectInfo" => await InvokeViewComponentDirectly("ProjectInfo", new { applicationId, applicationFormVersionId }),
                "ApplicantInfo" => await InvokeViewComponentDirectly("ApplicantInfo", new { applicationId, applicationFormVersionId }),
                "FundingAgreementInfo" => await InvokeViewComponentDirectly("FundingAgreementInfo", new { applicationId, applicationFormVersionId }),
                "PaymentInfo" => await InvokeViewComponentDirectly("PaymentInfo", new { applicationId, applicationFormVersionId }),
                "HistoryWidget" => await InvokeViewComponentDirectly("HistoryWidget", new { applicationId }),
                "ApplicationStatusWidget" => await InvokeViewComponentDirectly("ApplicationStatusWidget", new { applicationId }),
                "ApplicationAttachments" => await InvokeViewComponentDirectly("ApplicationAttachments", new { applicationId }),
                "CustomTabWidget" => await LoadCustomTabWidget(parameters),
                _ => throw new ArgumentException($"Unknown component: {component}")
            };
        }

        private async Task<IHtmlContent> LoadCustomTabWidget(ComponentParameters parameters)
        {
            var worksheetId = Guid.Parse(parameters.WorksheetIdStr);
            
            return await InvokeViewComponentDirectly(typeof(CustomTabWidgetViewComponent), new
            {
                instanceCorrelationId = parameters.InstanceCorrelationId,
                instanceCorrelationProvider = parameters.InstanceCorrelationProvider,
                sheetCorrelationId = parameters.SheetCorrelationId,
                sheetCorrelationProvider = parameters.SheetCorrelationProvider,
                uiAnchor = parameters.UiAnchor,
                name = parameters.Name,
                title = parameters.Title,
                worksheetId = worksheetId
            });
        }

        private static JsonResult CreateErrorResponse(Exception ex, string component)
        {
            return new JsonResult(new
            {
                success = false,
                error = ex.Message,
                component = component,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }

        #endregion

        #region Supporting Classes

        private sealed class ComponentParameters
        {
            public Guid InstanceCorrelationId { get; set; }
            public string InstanceCorrelationProvider { get; set; } = "";
            public string SheetCorrelationId { get; set; } = "";
            public string SheetCorrelationProvider { get; set; } = "";
            public string UiAnchor { get; set; } = "";
            public string Name { get; set; } = "";
            public string Title { get; set; } = "";
            public string WorksheetIdStr { get; set; } = "";
        }

        #endregion

        // Public overloads for different component invocation types
        private async Task<IHtmlContent> InvokeViewComponentDirectly(Type componentType, object arguments)
        {
            return await InvokeViewComponentCoreAsync(componentType: componentType, arguments: arguments);
        }

        private async Task<IHtmlContent> InvokeViewComponentDirectly(string componentName, object arguments)
        {
            return await InvokeViewComponentCoreAsync(componentName: componentName, arguments: arguments);
        }

        // Core method that handles both Type and string-based invocations
        private async Task<IHtmlContent> InvokeViewComponentCoreAsync(Type? componentType = null, string? componentName = null, object? arguments = null)
        {
            // Validate input parameters
            if (componentType == null && string.IsNullOrEmpty(componentName))
            {
                throw new ArgumentException("Either componentType or componentName must be provided");
            }

            using var writer = new StringWriter();

            // Create contexts
            var actionContext = new ActionContext(HttpContext, RouteData, PageContext.ActionDescriptor);
            var viewContext = new ViewContext(
                actionContext,
                new FakeView(),
                new ViewDataDictionary(ViewData),
                TempData,
                writer,
                new HtmlHelperOptions()
            );

            // Get and configure ViewComponent helper
            var viewComponentHelper = HttpContext.RequestServices.GetRequiredService<IViewComponentHelper>();
            if (viewComponentHelper is IViewContextAware contextAware)
            {
                contextAware.Contextualize(viewContext);
            }

            // Invoke based on available identifier
            var result = componentType != null
                ? await viewComponentHelper.InvokeAsync(componentType, arguments)
                : await viewComponentHelper.InvokeAsync(componentName!, arguments);

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

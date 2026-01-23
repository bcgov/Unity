using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DetailsModel : AbpPageModel
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
        public List<BoundWorksheet> CustomTabs { get; set; } = [];

        [BindProperty]
        public HashSet<string> ZoneStateSet { get; set; } = [];

        public DetailsModel(
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
            RenderFormIoToHtml = applicationForm.RenderFormIoToHtml;
            if (!string.IsNullOrEmpty(applicationFormSubmission.RenderedHTML) && RenderFormIoToHtml)
            {
                ApplicationFormSubmissionHtml = SanitizeFormIoHtml(applicationFormSubmission.RenderedHTML);
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

        private static string SanitizeFormIoHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var sanitizer = CreateFormIoSanitizer();
            return sanitizer.Sanitize(html);
        }

        private static HtmlSanitizer CreateFormIoSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith(new[]
            {
                "a", "abbr", "b", "blockquote", "br", "code", "dd", "div", "dl", "dt",
                "em", "fieldset", "form", "h1", "h2", "h3", "h4", "h5", "h6", "hr",
                "i", "img", "input", "label", "legend", "li", "ol", "option", "p",
                "pre", "select", "small", "span", "strong", "table", "tbody", "td",
                "textarea", "tfoot", "th", "thead", "tr", "u", "ul", "button",
                // Form.io required tags
                "canvas", "cite", "del", "details", "ins", "kbd", "mark", "q", "s", "samp",
                "section", "sub", "summary", "sup", "time", "var",
                // SVG for icons (CRITICAL)
                "svg", "path", "circle", "rect", "line", "polygon", "polyline", "g",
                "defs", "use", "symbol", "ellipse"
            });

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.UnionWith(new[]
            {
                "id", "class", "name", "value", "type", "placeholder", "title", "alt",
                "href", "src", "for", "role", "tabindex", "aria-*", "data-*",
                "checked", "selected", "disabled", "readonly", "required", "multiple",
                "min", "max", "step", "maxlength", "minlength", "size", "pattern", "style",
                "colspan", "rowspan", "scope", "accept", "autocomplete", "target",
                "rel", "download",
                // Form sizing and structure
                "rows", "cols", "width", "height", "open", "hidden", "datetime", "align", "valign",
                // SVG attributes (CRITICAL for icons)
                "viewBox", "xmlns", "fill", "stroke", "stroke-width", "d", "cx", "cy", "r",
                "x", "y", "x1", "y1", "x2", "y2", "points", "transform"
            });

            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.UnionWith(new[] { "http", "https", "mailto", "tel", "data" });

            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedCssProperties.UnionWith(new[]
            {
                "align-content",
                "align-items",
                "align-self",
                "background",
                "background-color",
                "border",
                "border-bottom",
                "border-bottom-color",
                "border-bottom-style",
                "border-bottom-width",
                "border-color",
                "border-left",
                "border-left-color",
                "border-left-style",
                "border-left-width",
                "border-radius",
                "border-right",
                "border-right-color",
                "border-right-style",
                "border-right-width",
                "border-style",
                "border-top",
                "border-top-color",
                "border-top-style",
                "border-top-width",
                "border-width",
                "box-shadow",
                "box-sizing",
                "color",
                "column-gap",
                "cursor",
                "display",
                "flex",
                "flex-basis",
                "flex-direction",
                "flex-grow",
                "flex-shrink",
                "flex-wrap",
                "font",
                "font-family",
                "font-size",
                "font-style",
                "font-weight",
                "gap",
                "height",
                "justify-content",
                "line-height",
                "margin",
                "margin-bottom",
                "margin-left",
                "margin-right",
                "margin-top",
                "max-height",
                "max-width",
                "min-height",
                "min-width",
                "padding",
                "padding-bottom",
                "padding-left",
                "padding-right",
                "padding-top",
                "text-align",
                "text-decoration",
                "text-transform",
                "vertical-align",
                "white-space",
                "width",
                "word-break",
                "word-wrap",
                // Critical layout and positioning
                "background-image", "background-position", "background-repeat", "background-size",
                "bottom", "float", "left", "letter-spacing", "list-style", "list-style-type",
                "opacity", "outline", "outline-color", "outline-style", "outline-width",
                "overflow", "overflow-x", "overflow-y", "position", "right", "text-indent",
                "text-overflow", "top", "visibility", "word-spacing", "z-index"
            });

            return sanitizer;
        }
    }

    public class BoundWorksheet
    {
        public WorksheetBasicDto? Worksheet { get; set; }
        public string UiAnchor { get; set; } = string.Empty;
        public uint? Order { get; set; } = 0;
    }
}

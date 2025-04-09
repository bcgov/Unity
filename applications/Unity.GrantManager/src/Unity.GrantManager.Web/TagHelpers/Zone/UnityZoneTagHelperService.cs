using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Zones;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.Microsoft.AspNetCore.Razor.TagHelpers;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.TagHelpers;

public class UnityZoneTagHelperService : AbpTagHelperService<UnityZoneTagHelper>
{
    private IHtmlGenerator HtmlGenerator;
    private IAbpTagHelperLocalizer _localizer { get; }
    private IFeatureChecker FeatureChecker { get; }
    private IPermissionChecker PermissionChecker { get; }
    private IZoneChecker ZoneChecker { get; }

    private bool _featureState = true;
    private bool _zoneState = true;
    private bool _readRermissionState = true;
    private bool _updateRermissionState = true;

    private bool _allRequirementsSatisfied => _featureState && _zoneState;

    public UnityZoneTagHelperService(
        IHtmlGenerator htmlGenerator,
        IAbpTagHelperLocalizer tagHelperLocalizer,
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker,
        IZoneChecker zoneChecker)
    {
        HtmlGenerator = htmlGenerator;
        _localizer = tagHelperLocalizer;
        FeatureChecker = featureChecker;
        PermissionChecker = permissionChecker;
        ZoneChecker = zoneChecker;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Set Requirement IDs
        TagHelper.PermissionRequirement = TagHelper.PermissionRequirement ?? TagHelper.Id;
        TagHelper.ZoneRequirement = TagHelper.ZoneRequirement ?? TagHelper.Id;

        await CheckRequirementsAsync();

        if (!TagHelper.Condition || !_allRequirementsSatisfied)
        {
            output.SuppressOutput();
            await CheckRequirementsAsync();
            AppendDebugHeader(output);
            return;
        }

        if (output.TagName == "zone")
        {
            output.TagName = "div";
            output.Attributes.Add("id", TagHelper.ElementId);
        }

        if (output.TagName == "zone-fieldset")
        {
            output.TagName = "fieldset";
            output.Attributes.Add("name", TagHelper.ElementId);
            
            // Toggle fieldset enabled/disabled on edit permission
            if (!_updateRermissionState)
            {
                output.Attributes.Add("disabled", "disabled");
            }

            AddFieldsetLegend(output);
        }

        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.AddClass("unity-zone");

        AppendDebugHeader(output);
        await output.GetChildContentAsync();
    }

    protected virtual void AddFieldsetLegend(TagHelperOutput output)
    {
        var legend = new TagBuilder("legend");
        legend.AddCssClass("h6 ps-1 fw-bold d-none");
        legend.InnerHtml.Append(TagHelper.ElementId);

        output.PreContent.AppendHtml(legend);
    }

    protected virtual void AppendDebugHeader(TagHelperOutput output)
    {
        var debugAlert = new TagBuilder("div");
        debugAlert.AddCssClass("alert shadow-sm alert-info zone-debugger-alert font-monospace m-2 d-none");
        debugAlert.Attributes.Add("role", "alert");

        var debugMessage = $@"
            <dl class=""row"">
                <dt class=""col-sm-3"">Zone Element ID</dt><dd class=""col-sm-9"">{TagHelper.ElementId}</dd>
                <dt class=""col-sm-3"">Form ID</dt><dd class=""col-sm-9"">
                    <a href=""/ApplicationForms/Mapping?ApplicationId={TagHelper.FormId}"" target=""_blank"" rel=""noopener noreferrer"">{TagHelper.FormId}<i class=""fa fa-external-link small"" aria-hidden=""true""></i></a>
                </dd>
                <dt class=""col-sm-3"">FeatureRequirement</dt><dd class=""col-sm-9"">{StatusBadge(_featureState)}{TagHelper.FeatureRequirement ?? "N/A"}{(TagHelper.Id == TagHelper.FeatureRequirement ? " (Inherited)" : string.Empty)}</dd>
                <dt class=""col-sm-3"">ZoneRequirement</dt><dd class=""col-sm-9"">{StatusBadge(_zoneState)}{TagHelper.ZoneRequirement ?? "N/A"}{(TagHelper.Id == TagHelper.ZoneRequirement ? " (Inherited)" : string.Empty)}</dd>
                <dt class=""col-sm-3"">ReadPermissionRequirement</dt><dd class=""col-sm-9"">{StatusBadge(_readRermissionState)}{TagHelper.PermissionRequirement ?? "N/A"}{(TagHelper.Id == TagHelper.PermissionRequirement ? " (Inherited)" : string.Empty)}</dd>
                <hr/>
                <dt class=""col-sm-3"">UpdatePermissionRequirement</dt><dd class=""col-sm-9"">{StatusBadge(_updateRermissionState)}{TagHelper.UpdatePermissionRequirement ?? "N/A"}</dd>
            </dl>";

        debugAlert.InnerHtml.SetHtmlContent(debugMessage);
        output.PreElement.AppendHtml(debugAlert);
    }

    private string StatusBadge(bool condition)
        => (condition ? "<span class=\"badge text-bg-primary\">PASS</span> " : "<span class=\"badge text-bg-secondary\">FAIL</span> ");

    protected async Task CheckRequirementsAsync()
    {
        if (!string.IsNullOrWhiteSpace(TagHelper.FeatureRequirement) 
            && !await FeatureChecker.IsEnabledAsync(TagHelper.FeatureRequirement))
        {
            _featureState = false;
        }

        if (!string.IsNullOrWhiteSpace(TagHelper.PermissionRequirement)
            && !await PermissionChecker.IsGrantedAsync(TagHelper.PermissionRequirement))
        {
            _readRermissionState = false;
        }

        // Skip zone checks if FormId is null
        if (TagHelper.FormId != Guid.Empty
            && !string.IsNullOrWhiteSpace(TagHelper.ZoneRequirement)
            && !await ZoneChecker.IsEnabledAsync(TagHelper.ZoneRequirement, TagHelper.FormId))
        {
            _zoneState = false;
        }

        if (!string.IsNullOrWhiteSpace(TagHelper.UpdatePermissionRequirement)
                && !await PermissionChecker.IsGrantedAsync(TagHelper.UpdatePermissionRequirement))
        {
            _updateRermissionState = false;
        }
    }
}
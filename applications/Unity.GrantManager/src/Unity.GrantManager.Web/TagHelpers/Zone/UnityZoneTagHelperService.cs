using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
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
        if (!TagHelper.Condition)
        {
            output.SuppressOutput();
        }

        // Set Requirement IDs
        TagHelper.PermissionRequirement = TagHelper.PermissionRequirement ?? TagHelper.Id;
        TagHelper.ZoneRequirement = TagHelper.ZoneRequirement ?? TagHelper.Id;
        // TODO: Fix Permission Configurations - bool allRequirementsSatisfied = await CheckRequirementsAsync();
        bool allRequirementsSatisfied = true;
        if (!allRequirementsSatisfied)
        {
            if (TagHelper.RenderMode == ConditionalRenderOutput.Suppress)
            {
                output.SuppressOutput();
                return;
            }

            if (TagHelper.RenderMode == ConditionalRenderOutput.Hide)
            {
                output.Attributes.AddClass("d-none");
            }
        }

        if (output.TagName == "zone")
        {
            output.TagName = "div";
            output.Attributes.Add("id", TagHelper.Id);
        }

        if (output.TagName == "zone-fieldset")
        {
            output.TagName = "fieldset";
            output.Attributes.Add("name", TagHelper.Id);
            
            // Toggle fieldset enabled/disabled on edit permission
            if (!string.IsNullOrWhiteSpace(TagHelper.UpdatePermissionRequirement)
                && !await PermissionChecker.IsGrantedAsync(TagHelper.UpdatePermissionRequirement))
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
        legend.AddCssClass("d-none");
        legend.AddCssClass("h6");
        legend.AddCssClass("ps-1");
        legend.AddCssClass("fw-bold");
        // TODO: Configure localizer
        // var legendText = _localizer.GetLocalizedText(TagHelper.Id);
        legend.InnerHtml.Append(TagHelper.Id);

        output.PreContent.AppendHtml(legend);
    }

    protected virtual void AppendDebugHeader(TagHelperOutput output)
    {
        var debugAlert = new TagBuilder("div");
        debugAlert.AddCssClass("alert");
        debugAlert.AddCssClass("alert-info");
        debugAlert.AddCssClass("zone-debugger-alert");
        debugAlert.AddCssClass("font-monospace");
        debugAlert.AddCssClass("m-2");
        debugAlert.AddCssClass("d-none");
        debugAlert.Attributes.Add("role", "alert");

        var debugMessage = "<dl class=\"row\">";
        debugMessage += $"<dt class=\"col-sm-3\">ZoneID</dt><dd class=\"col-sm-9\">{TagHelper.Id}</dd>";
        debugMessage += $"<dt class=\"col-sm-3\">ZoneRequirement</dt><dd class=\"col-sm-9\">{TagHelper.ZoneRequirement ?? "N/A"}</dd>";
        debugMessage += $"<dt class=\"col-sm-3\">FeatureRequirement</dt><dd class=\"col-sm-9\">{TagHelper.FeatureRequirement ?? "N/A"}</dd>";
        debugMessage += $"<dt class=\"col-sm-3\">ReadPermissionRequirement</dt><dd class=\"col-sm-9\">{TagHelper.PermissionRequirement ?? "N/A"}</dd>";
        debugMessage += $"<dt class=\"col-sm-3\">UpdatePermissionRequirement</dt><dd class=\"col-sm-9\">{TagHelper.UpdatePermissionRequirement ?? "N/A"}</dd>";
        debugMessage += "</dl>";

        debugAlert.InnerHtml.SetHtmlContent(debugMessage);
        
        output.PreElement.AppendHtml(debugAlert);
    }

    protected async Task<bool> CheckRequirementsAsync()
    {
        if (!string.IsNullOrWhiteSpace(TagHelper.FeatureRequirement) 
            && !await FeatureChecker.IsEnabledAsync(TagHelper.FeatureRequirement))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(TagHelper.PermissionRequirement)
            && !await PermissionChecker.IsGrantedAsync(TagHelper.PermissionRequirement))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(TagHelper.ZoneRequirement)
            && !await ZoneChecker.IsEnabledAsync(TagHelper.ZoneRequirement))
        {
            return false;
        }

        return true;
    }
}
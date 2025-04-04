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
        debugAlert.AddCssClass("font-monospace");
        debugAlert.AddCssClass("m-2");
        debugAlert.Attributes.Add("role", "alert");

        debugAlert.InnerHtml.SetHtmlContent($"Unity ZoneId : {TagHelper.Id}");
        
        output.PreElement.AppendHtml(debugAlert);
    }

    protected async Task<bool> CheckRequirementsAsync()
    {
        if (!string.IsNullOrWhiteSpace(TagHelper.FeatureRequirement) 
            && !await FeatureChecker.IsEnabledAsync(TagHelper.FeatureRequirement))
        {
            return false;
        }

        var permissionRequirement = TagHelper.PermissionRequirement ?? TagHelper.Id;
        if (!await PermissionChecker.IsGrantedAsync(permissionRequirement))
        {
            return false;
        }

        var zoneRequirement = TagHelper.ZoneRequirement ?? TagHelper.Id;
        if (!await ZoneChecker.IsEnabledAsync(zoneRequirement))
        {
            return false;
        }

        return true;
    }

    protected async Task<bool> IsEnabledAsync(string featureName)
    {
        return await PermissionChecker.IsGrantedAsync(TagHelper.PermissionRequirement ?? TagHelper.Id);
        // && await ZoneChecker.IsGrantedAsync(TagHelper.PermissionRequirement ?? TagHelper.Id)
    }
}
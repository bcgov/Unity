using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Localization;
using Unity.GrantManager.Zones;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.Microsoft.AspNetCore.Razor.TagHelpers;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.TagHelpers;

public class UnityZoneTagHelperService : AbpTagHelperService<UnityZoneTagHelper>
{
    private IFeatureChecker FeatureChecker { get; }
    private IPermissionChecker PermissionChecker { get; }
    private IZoneChecker ZoneChecker { get; }

    protected IStringLocalizer<GrantManagerResource> L { get; }

    private bool _featureState          = true;
    private bool _zoneState             = true;
    private bool _readPermissionState   = true;
    private bool _readCondition         = true;
    private bool _updatePermissionState = true;
    private bool _updateCondition       = true;

    private bool _readRequirementsSatisfied => _readCondition && _featureState && _zoneState && _readPermissionState;
    private bool _updateRequirementsSatisfied => _readRequirementsSatisfied && _updatePermissionState && _updateCondition;

    public UnityZoneTagHelperService(
        IFeatureChecker featureChecker,
        IPermissionChecker permissionChecker,
        IZoneChecker zoneChecker,
        IStringLocalizer<GrantManagerResource> stringLocalizer)
    {
        FeatureChecker = featureChecker;
        PermissionChecker = permissionChecker;
        ZoneChecker = zoneChecker;
        L = stringLocalizer;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Set Requirement IDs
        TagHelper.PermissionRequirement = TagHelper.PermissionRequirement ?? TagHelper.Id;
        TagHelper.ZoneRequirement = TagHelper.ZoneRequirement ?? TagHelper.Id;

        await CheckRequirementsAsync();

        if (!_readRequirementsSatisfied)
        {
            output.SuppressOutput();
            await CheckRequirementsAsync();
            AppendDebugHeader(output);
            return;
        }

        ConfigureOutputTag(output);
        AppendDebugHeader(output);
        await output.GetChildContentAsync();
    }

    private void ConfigureOutputTag(TagHelperOutput output)
    {
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
            if (!_updateRequirementsSatisfied)
            {
                output.Attributes.Add("disabled", "disabled");
            }

            AddFieldsetLegend(output);
        }

        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.AddClass("unity-zone");
    }

    protected async Task CheckRequirementsAsync()
    {
        _featureState = await CheckFeatureRequirementAsync();
        _readPermissionState = await CheckPermissionRequirementAsync(TagHelper.PermissionRequirement);
        _zoneState = await CheckZoneRequirementAsync();
        _updatePermissionState = await CheckPermissionRequirementAsync(TagHelper.UpdatePermissionRequirement);

        // Check if the conditions are explicitly set
        _readCondition = TagHelper.ReadCondition ?? true;
        _updateCondition = TagHelper.UpdateCondition ?? true;
    }

    private async Task<bool> CheckFeatureRequirementAsync()
    {
        if (string.IsNullOrWhiteSpace(TagHelper.FeatureRequirement))
            return true;

        return await FeatureChecker.IsEnabledAsync(TagHelper.FeatureRequirement);
    }

    private async Task<bool> CheckPermissionRequirementAsync(string? permissionName)
    {
        if (string.IsNullOrWhiteSpace(permissionName))
            return true;

        return await PermissionChecker.IsGrantedAsync(permissionName);
    }

    private async Task<bool> CheckZoneRequirementAsync()
    {
        if (string.IsNullOrWhiteSpace(TagHelper.ZoneRequirement) || TagHelper.FormId == Guid.Empty)
            return true;

        return await ZoneChecker.IsEnabledAsync(TagHelper.ZoneRequirement, TagHelper.FormId);
    }

    protected virtual void AddFieldsetLegend(TagHelperOutput output)
    {
        // Create the legend element with default CSS classes
        var legend = new TagBuilder("legend");
        legend.AddCssClass("h6 ps-1 pb-3 pt-2 mt-3 fw-bold");

        // Hide the legend if ShowLegend is false
        if (!TagHelper.ShowLegend)
        {
            legend.AddCssClass("d-none");
        }

        // Determine the legend content based on localization availability
        var localizedDisplayName = L[$"{TagHelper.Id}:DisplayName"];
        var legendContent = localizedDisplayName.ResourceNotFound
            ? L[TagHelper.Id]
            : localizedDisplayName;

        legend.InnerHtml.Append(legendContent);

        output.PreContent.AppendHtml(legend);
    }

    protected virtual void AppendDebugHeader(TagHelperOutput output)
    {
        var debugAlert = new TagBuilder("div");
        debugAlert.AddCssClass("alert shadow-sm alert-info zone-debugger-alert font-monospace m-2 d-none");
        debugAlert.Attributes.Add("role", "alert");

        var content = new HtmlContentBuilder();
        var dl = new TagBuilder("dl");
        dl.AddCssClass("row");

        // Basic information
        AddDefinitionItem(dl, "Zone Element ID", TagHelper.ElementId);
        AddFormIdWithLink(dl);

        // Feature and Zone requirements
        AddRequirementItem(dl, "FeatureRequirement", _featureState,
            TagHelper.FeatureRequirement, TagHelper.Id);
        AddRequirementItem(dl, "ZoneRequirement", _zoneState,
            TagHelper.ZoneRequirement, TagHelper.Id);

        AddSeparator(dl);

        // Permission requirements
        AddRequirementItem(dl, "ReadPermissionRequirement", _readPermissionState,
            TagHelper.PermissionRequirement, TagHelper.Id);
        AddDefinitionItem(dl, "CustomReadCondition", NotApplicableStatusBadge(TagHelper.ReadCondition));

        AddSeparator(dl);

        // Update requirements
        AddRequirementItem(dl, "UpdatePermissionRequirement", _updatePermissionState,
            TagHelper.UpdatePermissionRequirement, null);
        AddDefinitionItem(dl, "CustomUpdateCondition", NotApplicableStatusBadge(TagHelper.UpdateCondition));

        content.AppendHtml(dl);
        debugAlert.InnerHtml.AppendHtml(content);
        output.PreElement.AppendHtml(debugAlert);
    }

    private void AddFormIdWithLink(TagBuilder dl)
    {
        var formLink = new TagBuilder("a");
        formLink.Attributes.Add("href", $"/ApplicationForms/Mapping?ApplicationId={TagHelper.FormId}");
        formLink.Attributes.Add("target", "_blank");
        formLink.Attributes.Add("rel", "noopener noreferrer");
        formLink.InnerHtml.AppendHtml($"{TagHelper.FormId}");
        formLink.InnerHtml.AppendHtml("<i class=\"fa fa-external-link small\" aria-hidden=\"true\"></i>");
        AddDefinitionItem(dl, "Form ID", formLink);
    }

    private void AddRequirementItem(TagBuilder dl, string label, bool state, string? requirement, string? inheritFrom)
    {
        var content = new HtmlContentBuilder();
        content.AppendHtml(StatusBadge(state));
        content.Append(requirement ?? "N/A");

        // Add inheritance notice if applicable
        if (inheritFrom != null && inheritFrom == requirement)
        {
            content.Append(" (Inherited)");
        }

        AddDefinitionItem(dl, label, content);
    }

    private static void AddSeparator(TagBuilder container)
    {
        container.InnerHtml.AppendHtml("<hr class=\"mt-2\"/>");
    }

    private static void AddDefinitionItem(TagBuilder dl, string term, IHtmlContent content)
    {
        var dt = new TagBuilder("dt");
        dt.AddCssClass("col-sm-3");
        dt.InnerHtml.Append(term);

        var dd = new TagBuilder("dd");
        dd.AddCssClass("col-sm-9");
        dd.InnerHtml.AppendHtml(content);

        dl.InnerHtml.AppendHtml(dt);
        dl.InnerHtml.AppendHtml(dd);
    }

    private static void AddDefinitionItem(TagBuilder dl, string term, string content)
    {
        AddDefinitionItem(dl, term, new HtmlString(content));
    }

    private static IHtmlContent NotApplicableStatusBadge(bool? condition)
        => condition == null
            ? CreateBadge("NONE", "text-bg-light")
            : StatusBadge(condition);

    private static IHtmlContent StatusBadge(bool? condition)
        => condition == true
            ? CreateBadge("PASS", "text-bg-primary")
            : CreateBadge("FAIL", "text-bg-secondary");

    private static HtmlContentBuilder CreateBadge(string text, string styleClass)
    {
        var badge = new TagBuilder("span");
        badge.AddCssClass("badge");
        badge.AddCssClass(styleClass);
        badge.InnerHtml.Append(text);

        var content = new HtmlContentBuilder();
        content.AppendHtml(badge);
        content.Append(" "); // Add space after badge

        return content;
    }
}
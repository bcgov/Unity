using Microsoft.AspNetCore.Razor.TagHelpers;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers;

namespace Unity.GrantManager.Web.TagHelpers;

//[HtmlTargetElement("zone-control", Attributes ="asp-route-*",ParentTag ="", TagStructure = TagStructure.NormalOrSelfClosing)]

[HtmlTargetElement("zone")]
[HtmlTargetElement("zone-fieldset")]
public class UnityZoneTagHelper(UnityZoneTagHelperService tagHelperService)
    : AbpTagHelper<UnityZoneTagHelper, UnityZoneTagHelperService>(tagHelperService)
{
    public required string Id { get; set; }
    public ConditionalRenderOutput RenderMode { get; set; } = ConditionalRenderOutput.Hide;

    public string? PermissionRequirement { get; set; }
    public string? ZoneRequirement { get; set; }
    public string? FeatureRequirement { get; set; }

    [HtmlAttributeName("check-if")]
    public bool Condition { get; set; }
}

/// <summary>
/// 
/// </summary>
public enum ConditionalRenderOutput
{
    Hide,
    Suppress
}

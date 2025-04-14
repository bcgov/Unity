using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers;

namespace Unity.GrantManager.Web.TagHelpers;

//[HtmlTargetElement("zone-control", Attributes ="asp-route-*",ParentTag ="", TagStructure = TagStructure.NormalOrSelfClosing)]

[HtmlTargetElement("zone")]
[HtmlTargetElement("zone-fieldset")]
public class UnityZoneTagHelper(UnityZoneTagHelperService tagHelperService)
    : AbpTagHelper<UnityZoneTagHelper, UnityZoneTagHelperService>(tagHelperService)
{
    public required string Id { get; set; }
    
    public string ElementId => _elementId ??= Id.Replace('.', '_');
    private string? _elementId;
    
    public string? PermissionRequirement { get; set; }
    public string? UpdatePermissionRequirement { get; set; }
    public string? ZoneRequirement { get; set; }
    public string? FeatureRequirement { get; set; }
    public bool ShowLegend { get; set; } = false; // Can be removed once all zones are updated

    public Guid FormId { get; set; } = Guid.Empty;

    [HtmlAttributeName("check-if")]
    public bool Condition { get; set; } = true;
}
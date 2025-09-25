using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationFormConfigWidget;

public class ApplicationFormConfigWidgetViewModel
{
    public string? ConfigType { get; set; }

    public bool IsDirectApproval { get; set; }
    public AddressType? ElectoralDistrictAddressType { get; set; } = AddressType.PhysicalAddress;

    public List<SelectListItem> ElectoralDistrictAddressTypes { get; set; } = [];

    public string? Prefix { get; set; }
    
    public SuffixConfigType? SuffixType { get; set; }
    public List<SelectListItem> SuffixTypes { get; set; } = [];

    public static List<SelectListItem> FormatOptionsList(Dictionary<string, string> optionsList)
    {
        List<SelectListItem> optionsFormattedList = new();
        foreach (KeyValuePair<string, string> entry in optionsList)
        {
            optionsFormattedList.Add(new SelectListItem { Value = entry.Key, Text = entry.Value });
        }
        return optionsFormattedList;
    }

}

using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationFormConfigWidget;

public class ApplicationFormConfigWidgetViewModel
{
    public string? ConfigType { get; set; }

    public bool IsDirectApproval { get; set; }
    public AddressType? ElectoralDistrictAddressType { get; set; } = AddressType.PhysicalAddress;

    public List<SelectListItem> ElectoralDistrictAddressTypes { get; set; } = [];
}

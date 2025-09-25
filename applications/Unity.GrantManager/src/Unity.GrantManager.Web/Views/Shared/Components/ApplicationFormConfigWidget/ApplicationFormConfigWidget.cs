using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationFormConfigWidget;

[ViewComponent(Name = "ApplicationFormConfigWidget")]
[Widget(
    ScriptFiles = ["/Views/Shared/Components/ApplicationFormConfigWidget/Default.js"],
    StyleFiles = ["/Views/Shared/Components/ApplicationFormConfigWidget/Default.css"],
    RefreshUrl = "Widgets/ApplicationFormConfigWidget/Refresh",
    AutoInitialize = true
)]
public class ApplicationFormConfigWidget : AbpViewComponent
{
    /// <summary>
    /// Invokes the view component to render the application form configuration widget.
    /// </summary>
    /// <param name="configType"></param>
    /// <param name="applicationForm"></param>
    /// <returns>Form configuration widget</returns>
    public async Task<IViewComponentResult> InvokeAsync(string? configType, ApplicationFormDto? applicationForm)
    {
        await Task.CompletedTask;

        var viewModel = new ApplicationFormConfigWidgetViewModel()
        {
            ConfigType = configType,
            IsDirectApproval = applicationForm?.IsDirectApproval ?? false,
            ElectoralDistrictAddressType = applicationForm?.ElectoralDistrictAddressType ?? GrantApplications.AddressType.PhysicalAddress,
            ElectoralDistrictAddressTypes = LoadElectoralAddressOptions(),
            Prefix = applicationForm?.Prefix,
            SuffixType = applicationForm?.SuffixType,
            SuffixTypes = LoadSuffixOptions()
        };

        return View(viewModel);
    }

    /// <summary>
    /// Loads the available electoral district address types for the dropdown.
    /// </summary>
    /// <returns>Available electoral district address types</returns>
    private static List<SelectListItem> LoadElectoralAddressOptions()
    {
        List<SelectListItem> electoralDistrictAddressOptions = ApplicationForm
            .GetAvailableElectoralDistrictAddressTypes()
            .Select(x => new SelectListItem
            {
                Value = ((int)x.AddressType).ToString(),
                Text = x.DisplayName
            })
                .ToList();

        return electoralDistrictAddressOptions;
    }


    private static List<SelectListItem> LoadSuffixOptions()
    {
        List<SelectListItem> suffixOptions = ApplicationForm
            .GetAvailableSuffixTypes()
            .Select(x => new SelectListItem
            {
                Value = ((int)x.SuffixType).ToString(),
                Text = x.DisplayName
            })
                .ToList();

        return suffixOptions;
    }
}


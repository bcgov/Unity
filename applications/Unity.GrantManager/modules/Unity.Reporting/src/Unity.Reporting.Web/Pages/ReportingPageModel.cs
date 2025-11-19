using Unity.Reporting.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Reporting.Web.Pages;

/// <summary>
/// Base page model class for all Unity.Reporting Razor Pages providing common configuration and functionality.
/// Pre-configures localization resource and AutoMapper context for consistent behavior across all reporting pages.
/// All reporting-specific page models should inherit from this base class to ensure proper localization
/// and object mapping setup throughout the reporting web interface.
/// </summary>
/* Inherit your PageModel classes from this class.
 */
public abstract class ReportingPageModel : AbpPageModel
{
    /// <summary>
    /// Initializes a new instance of the ReportingPageModel with pre-configured localization and AutoMapper settings.
    /// Sets up the ReportingResource for consistent localization across all reporting pages
    /// and configures AutoMapper context to use the ReportingWebModule for object mapping operations.
    /// </summary>
    protected ReportingPageModel()
    {
        LocalizationResourceType = typeof(ReportingResource);
        ObjectMapperContext = typeof(ReportingWebModule);
    }
}

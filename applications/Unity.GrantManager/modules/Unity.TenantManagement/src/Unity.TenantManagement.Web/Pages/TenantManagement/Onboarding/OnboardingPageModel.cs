using Unity.TenantManagement.Web;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Onboarding;

public abstract class OnboardingPageModel : AbpPageModel
{
    protected OnboardingPageModel()
    {
        ObjectMapperContext = typeof(UnityTenantManagementWebModule);
    }
}

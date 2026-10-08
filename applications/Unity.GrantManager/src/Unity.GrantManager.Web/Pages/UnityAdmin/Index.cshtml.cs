using Microsoft.AspNetCore.Authorization;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unity.GrantManager.Identity;
using Unity.Modules.Shared.Permissions;
using Unity.Modules.Shared.Specializations;

using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Web.Pages.UnityAdmin
{
    [Authorize(IdentityConsts.ITAdminOrITOperationsPolicyName)]
    public partial class IndexModel(
        IUserTenantAppService userTenantAppService,
        ICurrentTenant currentTenant,
        ISpecializationChecker specializationChecker,
        ISettingProvider settingProvider) : GrantManagerPageModel
    {
        public List<string> Tenants { get; set; } = [];

        public string CurrentSelectedTenant { get; set; } = currentTenant?.Name ?? string.Empty;

        public bool IsRestricted { get; set; } = true;
        public bool ShowAIPrompts { get; set; }
        public bool ShowEndpoints { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAdminPageStateAsync();
            await LoadRetentionSettingsAsync(settingProvider);
        }

        private async Task LoadAdminPageStateAsync()
        {
            var isOnboarding = await specializationChecker.IsEnabledAsync(SpecializationConsts.Onboarding);
            ShowAIPrompts = !isOnboarding && User.IsInRole(IdentityConsts.ITOperationsRoleName);
            ShowEndpoints = !isOnboarding
                && (User.IsInRole(IdentityConsts.ITAdminRoleName) || User.IsInRole(IdentityConsts.ITOperationsRoleName));

            IsRestricted = !string.IsNullOrEmpty(CurrentSelectedTenant);
            var userTenants = await userTenantAppService.GetListAsync();
            Tenants = userTenants
                .Where(t => t.TenantName != null)
                .Select(t => t.TenantName!)
                .ToList();
        }
    }
}

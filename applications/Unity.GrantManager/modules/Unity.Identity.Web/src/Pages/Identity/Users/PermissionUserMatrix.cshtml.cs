using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Repositories;
using Unity.Notifications.Features;
using Unity.Notifications.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace Unity.Identity.Web.Pages.Identity.Users;

[Authorize(IdentityPermissions.Users.Default)]
public class PermissionUserMatrixModel(IPermissionRoleMatrixRepository repository, IPermissionDefinitionManager permissionDefinitionManager,
    IFeatureChecker featureChecker, ICurrentTenant currentTenant) : AbpPageModel
{
    public bool IsExpanded { get; private set; }
    public bool ShowNotDefined { get; private set; } = false;
    public required IList<PermissionUserMatrixRowDto> PermissionUserMatrix { get; set; }
    public required IList<UserInfoDto> Users { get; set; }

    public async Task OnGetAsync()
    {
        IsExpanded = Request.Query["Render"].ToString().Equals("Expanded", StringComparison.OrdinalIgnoreCase);
        ShowNotDefined = Request.Query["Show"].ToString().Equals("NotDefined", StringComparison.OrdinalIgnoreCase);

        var result = await repository.GetPermissionUserMatrixAsync();
        PermissionUserMatrix = result.Rows;
        Users = result.Users;

        var definedPermissionSet = await permissionDefinitionManager.GetPermissionsAsync();
        var definedPermissionNames = new HashSet<string>(definedPermissionSet.Select(x => x.Name));

        // This tenant flag only hides individual scheduling permissions; host reports keep all rows.
        if (currentTenant.IsAvailable
            && !await featureChecker.IsEnabledAsync(NotificationsFeatureConsts.IndividualScheduledNotification))
        {
            PermissionUserMatrix = PermissionUserMatrix
                .Where(item => !NotificationsPermissions.IsIndividualSchedulingPermission(item.PermissionName)).ToList();
        }

        foreach (var item in PermissionUserMatrix)
        {
            item.IsDefined = definedPermissionNames.Contains(item.PermissionName);

            var displayName = item.PermissionDisplayName;
            if (displayName.Length > 2 && displayName.StartsWith("L:"))
            {
                var parts = displayName.AsSpan(2).ToString().Split(',');
                if (parts.Length == 2)
                {
                    var resourceName = parts[0];
                    var name = parts[1];
                    var localizable = new LocalizableString(name, resourceName);
                    var localized = await localizable.LocalizeAsync(StringLocalizerFactory);
                    item.PermissionDisplayName = localized?.Value ?? displayName;
                }
            }
        }
    }
}

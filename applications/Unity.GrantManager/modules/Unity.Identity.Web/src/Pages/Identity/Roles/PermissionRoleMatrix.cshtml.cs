using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Repositories;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.GrantManager.Web.Pages.Identity.Roles;

public class PermissionRoleMatrixModel(IPermissionRoleMatrixRepository repository, IPermissionDefinitionManager permissionDefinitionManager) : AbpPageModel
{
    public bool IsExpanded { get; private set; }
    public bool ShowNotDefined { get; private set; } = false;
    public required IList<PermissionRoleMatrixDto> PermissionRoleMatrix { get; set; }

    public async Task OnGetAsync()
    {
        // Check if the query parameter "Render" is set to "Expanded"
        IsExpanded = Request.Query["Render"].ToString().Equals("Expanded", StringComparison.OrdinalIgnoreCase);
        ShowNotDefined = Request.Query["Show"].ToString().Equals("NotDefined", StringComparison.OrdinalIgnoreCase);

        PermissionRoleMatrix = await repository.GetPermissionRoleMatrixAsync();

        var definedPermissionSet = await permissionDefinitionManager.GetPermissionsAsync();
        var definedPermissionNames = new HashSet<string>(definedPermissionSet.Select(x => x.Name));

        foreach (var item in PermissionRoleMatrix)
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
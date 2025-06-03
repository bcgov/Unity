using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Repositories;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Localization;

namespace Unity.GrantManager.Web.Pages.Identity.Roles;

public class PermissionRoleMatrixModel(IPermissionRoleMatrixRepository repository) : AbpPageModel
{
    public bool IsExpanded { get; private set; }
    public required IList<PermissionRoleMatrixDto> PermissionRoleMatrix { get; set; }

    public async Task OnGetAsync()
    {
        // Check if the query parameter "Render" is set to "Expanded"
        IsExpanded = Request.Query["Render"].ToString().Equals("Expanded", StringComparison.OrdinalIgnoreCase);

        PermissionRoleMatrix = await repository.GetPermissionRoleMatrixAsync();
        foreach (var item in PermissionRoleMatrix.Where(item => item.PermissionDisplayName.StartsWith("L:")))
        {
            var parts = item.PermissionDisplayName[2..].Split(',');
            if (parts.Length == 2)
            {
                var resourceName = parts[0];
                var name = parts[1];
                var displayName = new LocalizableString(name, resourceName);
                item.PermissionDisplayName = (await displayName.LocalizeAsync(StringLocalizerFactory))?.Value ?? item.PermissionDisplayName;
            }
        }
    }
}
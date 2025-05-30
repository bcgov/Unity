﻿using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.UI.Navigation;

namespace Unity.Reporting.Web.Menus;

public class ReportingMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        await ConfigureReportingMenuAsync(context);
    }

    private static Task ConfigureReportingMenuAsync(MenuConfigurationContext context)
    {
        //Add main menu items.
        context.Menu.AddItem(
            new ApplicationMenuItem(
                    ReportingMenus.Prefix,
                    displayName: "Reporting Admin",
                    "~/ReportingAdmin",
                    requiredPermissionName: IdentityConsts.ITAdminPermissionName
        ));
        context.Menu.AddItem(
            new ApplicationMenuItem(
                    ReportingMenus.Prefix,
                    displayName: "Reconciliation",
                    "~/TenantManagement/Reconciliation",
                    requiredPermissionName: IdentityConsts.ITAdminPermissionName
        ));
        return Task.CompletedTask;
    }
}

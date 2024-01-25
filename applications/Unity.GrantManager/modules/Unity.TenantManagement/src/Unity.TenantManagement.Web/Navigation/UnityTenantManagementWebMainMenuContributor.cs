using System.Threading.Tasks;
using Volo.Abp.TenantManagement.Localization;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Authorization.Permissions;

namespace Unity.TenantManagement.Web.Navigation;

public class AbpTenantManagementWebMainMenuContributor : IMenuContributor
{
    public virtual Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name != StandardMenus.Main)
        {
            return Task.CompletedTask;
        }

        var administrationMenu = context.Menu.GetAdministration();

        var l = context.GetLocalizer<AbpTenantManagementResource>();

        var tenantManagementMenuItem = new ApplicationMenuItem(TenantManagementMenuNames.GroupName, l["Menu:TenantManagement"], icon: "fa fa-users");
        administrationMenu.AddItem(tenantManagementMenuItem);

        tenantManagementMenuItem.AddItem(new ApplicationMenuItem(TenantManagementMenuNames.Tenants, l["Tenants"], url: "~/TenantManagement/Tenants").RequirePermissions(TenantManagementPermissions.Tenants.Default));

        return Task.CompletedTask;
    }
}

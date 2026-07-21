using System.Threading.Tasks;
using Unity.Modules.Shared.Navigation;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.TenantManagement.Localization;
using Volo.Abp.UI.Navigation;

namespace Unity.TenantManagement.Web.Navigation;

public class AbpTenantManagementWebMainMenuContributor : IMenuContributor
{
    public virtual async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name != StandardMenus.Main)
        {
            return;
        }

        var administrationMenu = context.Menu.GetAdministration();

        var l = context.GetLocalizer<AbpTenantManagementResource>();

        var tenantManagementMenuItem = new ApplicationMenuItem(TenantManagementMenuNames.GroupName, l["Menu:TenantManagement"], icon: "fa fa-users");
        administrationMenu.AddItem(tenantManagementMenuItem);

        await tenantManagementMenuItem.AddItemAsync(
            context.ServiceProvider,
            new ApplicationMenuItem(TenantManagementMenuNames.Tenants, l["Tenants"], url: "~/TenantManagement/Tenants")
                .OnlyWhenInRole(IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName)
        );
    }
}

using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.UI.Navigation;

namespace Unity.AI.Web.Menus;

public class AIMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        context.Menu.AddItem(new ApplicationMenuItem(
            name: AIMenus.Prompts,
            displayName: "AI Prompts",
            url: "~/Prompts",
            icon: "fl fl-ai-prompts",
            order: 900,
            requiredPermissionName: IdentityConsts.ITOperationsPermissionName
        ));

        return Task.CompletedTask;
    }
}

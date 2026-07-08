using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.AI.Localization;
using Unity.AI.Permissions;
using Unity.Modules.Shared.Navigation;
using Unity.Modules.Shared.Specializations;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Features;
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

    private static async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<AIResource>();
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        var specializationChecker = context.ServiceProvider.GetRequiredService<ISpecializationChecker>();
        if (!await specializationChecker.IsEnabledAsync(SpecializationConsts.Onboarding))
        {
            await context.AddItemAsync(new ApplicationMenuItem(
                name: AIMenus.Prompts,
                displayName: "AI Prompts",
                url: "~/Prompts",
                icon: "fl fl-ai-prompts",
                order: 900
            ).OnlyWhenInRole(IdentityConsts.ITOperationsRoleName));
        }

        if (await featureChecker.IsEnabledAsync("Unity.AIReporting"))
        {
            context.Menu.AddItem(new ApplicationMenuItem(
                name: AIMenus.Reporting,
                displayName: l["Menu:AIReporting"],
                url: "~/AIReporting",
                icon: "fl fl-view-dashboard",
                order: 9,
                requiredPermissionName: AIPermissions.Reporting.ReportingDefault
            ));
        }
    }
}

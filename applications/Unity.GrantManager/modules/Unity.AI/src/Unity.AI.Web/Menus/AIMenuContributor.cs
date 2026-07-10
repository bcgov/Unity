using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.AI.Localization;
using Unity.AI.Permissions;
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
            context.Menu.AddItem(new ApplicationMenuItem(
                name: AIMenus.Prompts,
                displayName: "AI Prompts",
                url: "~/Prompts",
                icon: "fl fl-ai-prompts",
                order: 900,
                requiredPermissionName: IdentityConsts.ITOperationsPermissionName
            ));
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

        if (await featureChecker.IsEnabledAsync("Unity.AI.FormMapping"))
        {
            context.Menu.AddItem(new ApplicationMenuItem(
                name: AIMenus.FormMapping,
                displayName: l["Permission:AI.ViewFormMapping"],
                url: "~/ApplicationForms",
                icon: "fl fl-map",
                order: 10,
                requiredPermissionName: AIPermissions.Analysis.GenerateFormMapping
            ));
        }

        if (await featureChecker.IsEnabledAsync("Unity.AI.FormWorksheet"))
        {
            context.Menu.AddItem(new ApplicationMenuItem(
                name: AIMenus.FormMapping + ".Worksheet",
                displayName: l["Permission:AI.ViewFormWorksheet"],
                url: "~/ApplicationForms",
                icon: "fl fl-ai-prompts",
                order: 11,
                requiredPermissionName: AIPermissions.Analysis.GenerateFormWorksheet
            ));
        }
    }
}

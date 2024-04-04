using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.UX2.Components.Toolbar.LanguageSwitch;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.UX2.Components.Toolbar.UserMenu;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.Toolbars;
using Volo.Abp.Localization;
using Volo.Abp.Users;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Toolbars;

public class UnityThemeMainTopToolbarContributor : IToolbarContributor
{
    public async Task ConfigureToolbarAsync(IToolbarConfigurationContext context)
    {
        if (context.Toolbar.Name != StandardToolbars.Main)
        {
            return;
        }

        if (!(context.Theme is UnityUX2Theme))
        {
            return;
        }

        var languageProvider = context.ServiceProvider.GetService<ILanguageProvider>();

        var languages = await languageProvider.GetLanguagesAsync();
        if (languages.Count > 1)
        {
            context.Toolbar.Items.Add(new ToolbarItem(typeof(LanguageSwitchViewComponent)));
        }

        if (context.ServiceProvider.GetRequiredService<ICurrentUser>().IsAuthenticated)
        {
            context.Toolbar.Items.Add(new ToolbarItem(typeof(UserMenuViewComponent)));
        }
    }
}

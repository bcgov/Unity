using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.AspNetCore.Mvc.UI.Themes.Standard.Components.Toolbar.LanguageSwitch;
using Unity.AspNetCore.Mvc.UI.Themes.Standard.Components.Toolbar.UserMenu;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.Toolbars;
using Volo.Abp.Localization;
using Volo.Abp.Users;

namespace Unity.AspNetCore.Mvc.UI.Themes.Standard.Toolbars;

public class StandardThemeMainTopToolbarContributor : IToolbarContributor
{
    public async Task ConfigureToolbarAsync(IToolbarConfigurationContext context)
    {
        if (context.Toolbar.Name != StandardToolbars.Main)
        {
            return;
        }

        if (context.Theme is not StandardTheme)
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

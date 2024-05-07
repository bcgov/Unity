using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using Volo.Abp.DependencyInjection;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2;

[ThemeName(Name)]
public class UnityUX2Theme : ITheme, ITransientDependency
{
    public const string Name = "UX2";
    public virtual string GetLayout(string name, bool fallbackToDefault = true)
    {

        switch (name)
        {
            case StandardLayouts.Application:
                return "~/Themes/UX2/Layouts/Application.cshtml";
            case StandardLayouts.Account:
                return "~/Themes/UX2/Layouts/Account.cshtml";
            case StandardLayouts.Empty:
                return "~/Themes/UX2/Layouts/Empty.cshtml";
            default:
                return fallbackToDefault ? "~/Themes/UX2/Layouts/Application.cshtml" : null;

        }
    }
}

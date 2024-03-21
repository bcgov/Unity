using Volo.Abp.AspNetCore.Mvc.UI.Layout;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using Volo.Abp.DependencyInjection;

namespace Unity.AspNetCore.Mvc.UI.Themes;

[ThemeName(Name)]
public class UnityTheme : ITheme, ITransientDependency
{
    public const string Name = "Unity";
    public virtual string GetLayout(string name, bool fallbackToDefault = true)
    {

            switch (name)
            {
                case StandardLayouts.Application:
                    return "~/Themes/Standard/Layouts/Application.cshtml";
                case StandardLayouts.Account:
                    return "~/Themes/Standard/Layouts/Account.cshtml";
                case StandardLayouts.Empty:
                    return "~/Themes/Standard/Layouts/Empty.cshtml";
                default:
                    return fallbackToDefault ? "~/Themes/Standard/Layouts/Application.cshtml" : null;
            
        }
    }
}

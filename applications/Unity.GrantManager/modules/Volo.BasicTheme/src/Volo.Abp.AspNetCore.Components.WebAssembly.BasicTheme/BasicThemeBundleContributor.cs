using Volo.Abp.Bundling;

namespace Volo.Abp.AspNetCore.Components.WebAssembly.BasicTheme;

public class BasicThemeBundleContributor : IBundleContributor
{
    public void AddScripts(BundleContext context)
    {
        //Placeholder. Nothing to do here yet.
    }

    public void AddStyles(BundleContext context)
    {
        context.Add("_content/Volo.Abp.AspNetCore.Components.Web.BasicTheme/libs/abp/css/theme.css");
    }
}

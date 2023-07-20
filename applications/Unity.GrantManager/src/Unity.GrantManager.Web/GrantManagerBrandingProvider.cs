using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Web;

[Dependency(ReplaceServices = true)]
public class GrantManagerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "Unity Grant Manager";
}

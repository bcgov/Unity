using Volo.Abp.Settings;

namespace Unity.GrantManager.Settings;

public class GrantManagerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(GrantManagerSettings.MySetting1));
    }
}

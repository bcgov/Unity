using Volo.Abp.Settings;

namespace Unity.AI.Settings;

public class AISettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        // Tenant AI configuration settings are defined in AB#32291
    }
}

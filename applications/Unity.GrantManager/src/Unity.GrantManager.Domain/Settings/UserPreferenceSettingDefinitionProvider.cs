using Volo.Abp.Settings;
using Unity.GrantManager.Settings;

public class UserPreferenceSettingDefinitionProvider
    : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(
                SettingsConstants.UserEmailSettings.EmailBlockCommentTagging,
                defaultValue: "false"
            ).WithProviders("U"),

            new SettingDefinition(
                SettingsConstants.UserEmailSettings.EmailBlockAssigned,
                defaultValue: "false"
            ).WithProviders("U")
        );
    }
}
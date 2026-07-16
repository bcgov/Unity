using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;

namespace Unity.TenantManagement.Onboarding;

public class OnboardingColumnConfigSettingDefinitionProvider : SettingDefinitionProvider
{
    private static SettingDefinition OnboardingDef(string name) =>
        new SettingDefinition(name, defaultValue: null, isVisibleToClients: false, isInherited: false, isEncrypted: false)
            .WithProviders(GlobalSettingValueProvider.ProviderName, UserSettingValueProvider.ProviderName);

    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            OnboardingDef(OnboardingColumnConfigSettings.TenantNameFieldKey),
            OnboardingDef(OnboardingColumnConfigSettings.SuperUsersFieldKey),
            OnboardingDef(OnboardingColumnConfigSettings.BranchFieldKey),
            OnboardingDef(OnboardingColumnConfigSettings.FeaturesFieldKey),
            OnboardingDef(OnboardingColumnConfigSettings.MinistryFieldKey),
            OnboardingDef(OnboardingColumnConfigSettings.ProgramAreaFieldKey)
        );
    }
}

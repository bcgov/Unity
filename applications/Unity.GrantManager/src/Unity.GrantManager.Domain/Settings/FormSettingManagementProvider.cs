using Volo.Abp.DependencyInjection;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Settings;

public class FormSettingManagementProvider(ISettingManagementStore store) : SettingManagementProvider(store), ITransientDependency
{
    public const string ProviderName = "F";
    public override string Name => ProviderName;
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Settings;
public class FormSettingValueProvider(ISettingStore store) : SettingValueProvider(store)
{
    public const string ProviderName = "F";
    public override string Name => ProviderName;

    public override Task<List<SettingValue>> GetAllAsync(SettingDefinition[] settings)
    {
        throw new NotImplementedException();
    }

    public override Task<string?> GetOrNullAsync(SettingDefinition setting)
    {
        throw new NotImplementedException();
    }
}

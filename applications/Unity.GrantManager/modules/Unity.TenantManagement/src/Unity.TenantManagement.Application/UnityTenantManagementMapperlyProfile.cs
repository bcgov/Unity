#nullable enable

using Volo.Abp.Mapperly;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement;

public class TenantToTenantDtoMapper : MapperBase<Tenant, TenantDto>
{
    public override TenantDto Map(Tenant source)
    {
        var destination = new TenantDto();
        Map(source, destination);
        return destination;
    }

    public override void Map(Tenant source, TenantDto destination)
    {
        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.ConcurrencyStamp = source.ConcurrencyStamp;
        destination.CasClientCode = GetExtraProperty(source, "CasClientCode") ?? string.Empty;
        destination.Division = GetExtraProperty(source, "Division") ?? string.Empty;
        destination.Branch = GetExtraProperty(source, "Branch") ?? string.Empty;
        destination.Description = GetExtraProperty(source, "Description") ?? string.Empty;

        foreach (var kvp in source.ExtraProperties)
        {
            destination.ExtraProperties[kvp.Key] = kvp.Value;
        }
    }

    private static string? GetExtraProperty(Tenant tenant, string key)
    {
        return tenant.ExtraProperties.TryGetValue(key, out var value) ? value?.ToString() : null;
    }
}

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.Mapperly;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement;

public class TenantToTenantDtoMapper : MapperBase<Tenant, TenantDto>
{
    // Property names camelCased to match the rest of the DTO's JSON shape on the wire; enum
    // values are left as their declared names (Waiting/Success/Failure/Error) rather than
    // camelCased, since Index.js uses them directly as localization key suffixes
    // (TenantList:PostCreationStatus:Success etc.).
    private static readonly JsonSerializerOptions SectionsJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

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
        destination.DisplayName = GetExtraProperty(source, "DisplayName") ?? string.Empty;
        destination.CasClientCode = GetExtraProperty(source, "CasClientCode") ?? string.Empty;
        destination.LicencePlate = GetExtraProperty(source, "LicencePlate") ?? string.Empty;
        destination.Division = GetExtraProperty(source, "Division") ?? string.Empty;
        destination.Branch = GetExtraProperty(source, "Branch") ?? string.Empty;
        destination.Description = GetExtraProperty(source, "Description") ?? string.Empty;

        // Built fresh from the flat ExtraProperties fields (not stored as JSON on the entity
        // itself - see TenantPostCreationSectionsExtensions) - this JSON string only ever goes
        // out to the client, it's never written back into Tenant.ExtraProperties.
        destination.Sections = JsonSerializer.Serialize(source.GetPostTenantCreationSections(), SectionsJsonOptions);

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

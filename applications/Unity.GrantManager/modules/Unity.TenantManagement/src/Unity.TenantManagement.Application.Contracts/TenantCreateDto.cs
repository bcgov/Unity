#nullable enable
namespace Unity.TenantManagement;

public class TenantCreateDto : TenantCreateOrUpdateDtoBase
{
    public string UserIdentifier { get; set; } = string.Empty;
    /// <summary>Comma-separated ABP feature keys to enable on the new tenant (e.g. "Unity.Payments,Unity.Reporting").</summary>
    public string? FeatureKeys { get; set; }
}

#nullable enable
namespace Unity.TenantManagement;

public class TenantConnectionStringsDto
{
    public string? TenantConnectionString { get; set; }
    public string? ReadOnlyConnectionString { get; set; }
}

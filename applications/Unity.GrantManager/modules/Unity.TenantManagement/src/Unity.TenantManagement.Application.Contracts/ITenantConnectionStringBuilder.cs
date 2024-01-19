using Volo.Abp.Application.Services;

namespace Unity.TenantManagement.Application.Contracts
{
    public interface ITenantConnectionStringBuilder : IApplicationService
    {
        string Build(string tenantName);
    }
}

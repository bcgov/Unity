using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.TenantManagement.Application.Contracts
{
    public interface ITenantConnectionStringBuilder : IApplicationService
    {
        Task<TenantDbCredentials> GenerateCredentialsAsync();

        string Build(string tenantName, TenantDbCredentials credentials);
    }
}

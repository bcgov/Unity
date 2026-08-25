using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.TenantManagement.Application.Contracts
{
    public interface ITenantConnectionStringBuilder : IApplicationService
    {
        Task<TenantDbCredentials> GenerateCredentialsAsync();

        TenantDbCredentials GenerateReadOnlyCredentials(TenantDbCredentials credentials);

        string Build(string tenantName, TenantDbCredentials credentials);
    }
}

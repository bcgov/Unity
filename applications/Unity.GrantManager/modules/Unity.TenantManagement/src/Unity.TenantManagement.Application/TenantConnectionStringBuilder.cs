using Microsoft.Extensions.Configuration;
using Unity.TenantManagement.Application.Contracts;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.TenantManagement.Application
{
    [RemoteService(false)]
    public class TenantConnectionStringBuilder : ApplicationService, ITenantConnectionStringBuilder
    {
        private readonly IConfiguration _configuration;

        public TenantConnectionStringBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Build(string tenantName)
        {
            var connectionString = _configuration.GetConnectionString(UnityTenantManagementConsts.TenantConnectionStringName);

            return connectionString == null
                ? throw new UserFriendlyException("Connection string configuration error")
                : connectionString
                .Replace(UnityTenantManagementConsts.TenantConnectionStringTenantDb, PrepTenantName(tenantName));
        }

        private static string PrepTenantName(string tenantName)
        {
            return tenantName.Trim().Replace(" ", "");
        }
    }
}

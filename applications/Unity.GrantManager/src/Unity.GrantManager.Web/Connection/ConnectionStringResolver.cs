using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Connection
{
    [Dependency(ReplaceServices = true)]
    public class MultiTenantConnectionStringResolver : DefaultConnectionStringResolver
    {
        private readonly ICurrentUser _currentUser;
        private readonly ICurrentTenant _currentTenant;
        private readonly IServiceProvider _serviceProvider;

        public MultiTenantConnectionStringResolver(
            IOptionsMonitor<AbpDbConnectionOptions> options,
            ICurrentTenant currentTenant,
            IServiceProvider serviceProvider,
            ICurrentUser currentUser)

            : base(options)
        {
            _currentTenant = currentTenant;
            _serviceProvider = serviceProvider;
            _currentUser = currentUser;
        }

        public override async Task<string> ResolveAsync(string? connectionStringName = null)
        {
            if (connectionStringName == "Tenant"
                && _currentUser.TenantId != null)
            {
                _currentTenant.Change(_currentUser.TenantId);
            }

            if (_currentTenant.Id == null)
            {
                //No current tenant, fallback to default logic
                return await base.ResolveAsync(connectionStringName);
            }

            var tenant = await FindTenantConfigurationAsync(_currentTenant.Id.Value);

            if (tenant == null || tenant.ConnectionStrings.IsNullOrEmpty())
            {
                //Tenant has not defined any connection string, fallback to default logic
                return await base.ResolveAsync(connectionStringName);
            }

            var tenantDefaultConnectionString = tenant.ConnectionStrings?.Default;

            //Requesting default connection string...
            if (connectionStringName == null ||
                connectionStringName == ConnectionStrings.DefaultConnectionStringName)
            {
                //Return tenant's default or global default
                return !tenantDefaultConnectionString.IsNullOrWhiteSpace()
                    ? tenantDefaultConnectionString!
                    : Options.ConnectionStrings.Default!;
            }

            //Requesting specific connection string...
            var connString = tenant.ConnectionStrings?.GetOrDefault(connectionStringName);
            if (!connString.IsNullOrWhiteSpace())
            {
                //Found for the tenant
                return connString!;
            }

            //Fallback to the mapped database for the specific connection string
            var database = Options.Databases.GetMappedDatabaseOrNull(connectionStringName);
            if (database != null && database.IsUsedByTenants)
            {
                connString = tenant.ConnectionStrings?.GetOrDefault(database.DatabaseName);
                if (!connString.IsNullOrWhiteSpace())
                {
                    //Found for the tenant
                    return connString!;
                }
            }

            //Fallback to tenant's default connection string if available
            if (!tenantDefaultConnectionString.IsNullOrWhiteSpace())
            {
                return tenantDefaultConnectionString!;
            }

            return await base.ResolveAsync(connectionStringName);
        }

        [Obsolete("Use ResolveAsync method.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Info Code Smell", "S1133:Deprecated code should be removed", Justification = "<Pending>")]
        public override string Resolve(string? connectionStringName = null)
        {
            return ResolveAsync(connectionStringName).Result;
        }

        protected virtual async Task<TenantConfiguration?> FindTenantConfigurationAsync(Guid tenantId)
        {
            using var serviceScope = _serviceProvider.CreateScope();
            var tenantStore = serviceScope
                .ServiceProvider
                .GetRequiredService<ITenantStore>();

            return await tenantStore.FindAsync(tenantId);
        }
    }
}

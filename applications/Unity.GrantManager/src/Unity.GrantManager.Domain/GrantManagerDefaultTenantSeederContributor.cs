using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager
{
    public class GrantManagerDefaultTenantSeederContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly ITenantManager _tenantManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly IdentityUserManager _identityUserManager;
        private readonly IConfiguration _configuration;
        private readonly ITenantUserRepository _tenantUserRepository;

        public GrantManagerDefaultTenantSeederContributor(ITenantManager tenantManager,
            ITenantRepository tenantRepository,
            IConfiguration configuration,
            ITenantUserRepository tenantUserRepository,
            IdentityUserManager identityUserManager)
        {
            _tenantManager = tenantManager;
            _tenantRepository = tenantRepository;
            _identityUserManager = identityUserManager;
            _configuration = configuration;
            _tenantUserRepository = tenantUserRepository;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            // The migrators run host first then tenants

            if (context.TenantId == null)
            {
                // Read the configuration and populate any default tenants
                var tenant = await _tenantRepository.FindByNameAsync("Default");

                if (tenant == null)
                {
                    var tenantConnectionString = _configuration.GetConnectionString("Tenant");
                    if (tenantConnectionString != null)
                    {
                        var newTenant = await _tenantManager.CreateAsync("Default");
                        newTenant.ConnectionStrings.Add(new TenantConnectionString(newTenant.Id, "default", tenantConnectionString));
                        await _tenantRepository.InsertAsync(newTenant, true);
                    }
                }
            }
            else
            {
                // Seed an account in the host db for each user, and a local user in each tenant
                string[] avengers = new string[] { "Steve.Rogers", "Bruce.Banner", "Natasha.Romanoff" };

                foreach (var avenger in avengers)
                {
                    var identityUser = await _identityUserManager.FindByNameAsync(avenger);
                    var split = avenger.Split('.');

                    if (identityUser == null)
                    {                        
                        identityUser = new(Guid.NewGuid(), avenger, $"{avenger.ToLower()}@example.com", context.TenantId)
                        {
                            Name = split[0],
                            Surname = split[1]
                        };
                        identityUser.SetProperty("OidcSub", $"{avenger.ToLower()}@avengers");
                        identityUser.SetProperty("DisplayName", $"{split[1]}, {split[0]}:Avengers");
                        await _identityUserManager.CreateAsync(identityUser);
                    }

                    var oidcProp = identityUser.GetProperty("OidcSub")?.ToString();
                    var displayName = identityUser.GetProperty("DisplayName")?.ToString();
                    var propFallback = Guid.NewGuid().ToString();

                    var tenantUser = await _tenantUserRepository.FindByOidcSub(oidcProp ?? propFallback);
                    if (tenantUser == null)
                    {
                        await _tenantUserRepository.InsertAsync(new User()
                        {
                            Id = identityUser.Id,
                            OidcDisplayName = displayName ?? identityUser.Name,
                            OidcSub = oidcProp ?? propFallback,
                            FullName = $"{identityUser.Name} {identityUser.Surname}",
                            Badge = Utils.CreateUserBadge(identityUser)
                        });
                    }
                }                
            }
        }
    }
}



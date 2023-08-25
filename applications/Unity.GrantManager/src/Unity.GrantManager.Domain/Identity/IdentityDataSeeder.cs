using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp;
using Volo.Abp.PermissionManagement;

namespace Unity.GrantManager.Identity
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IdentityDataSeeder), typeof(IIdentityDataSeeder))]
    public class IdentityDataSeeder : ITransientDependency, IIdentityDataSeeder
    {
        protected IGuidGenerator GuidGenerator { get; }
        protected IIdentityRoleRepository RoleRepository { get; }
        protected IIdentityUserRepository UserRepository { get; }
        protected ILookupNormalizer LookupNormalizer { get; }
        protected IdentityUserManager UserManager { get; }
        protected IdentityRoleManager RoleManager { get; }
        protected ICurrentTenant CurrentTenant { get; }
        protected IOptions<IdentityOptions> IdentityOptions { get; }

        protected IPermissionManager PermissionManager { get; }

        public IdentityDataSeeder(
            IGuidGenerator guidGenerator,
            IIdentityRoleRepository roleRepository,
            IIdentityUserRepository userRepository,
            ILookupNormalizer lookupNormalizer,
            IdentityUserManager userManager,
            IdentityRoleManager roleManager,
            ICurrentTenant currentTenant,
            IOptions<IdentityOptions> identityOptions,
            IPermissionManager permissionManager)
        {
            GuidGenerator = guidGenerator;
            RoleRepository = roleRepository;
            UserRepository = userRepository;
            LookupNormalizer = lookupNormalizer;
            UserManager = userManager;
            RoleManager = roleManager;
            CurrentTenant = currentTenant;
            IdentityOptions = identityOptions;
            PermissionManager = permissionManager;
        }

        [UnitOfWork]
        public virtual async Task<IdentityDataSeedResult> SeedAsync(
            string adminEmail,
            string adminPassword,
            Guid? tenantId = null)
        {
            Check.NotNullOrWhiteSpace(adminEmail, nameof(adminEmail));
            Check.NotNullOrWhiteSpace(adminPassword, nameof(adminPassword));

            using (CurrentTenant.Change(tenantId))
            {
                await IdentityOptions.SetAsync();

                var result = new IdentityDataSeedResult();
                
                foreach (var role in UnityRoles.DefinedRoles)
                {
                    await CreateRoleAsync(role, tenantId);
                }

                return result;
            }
        }

        private async Task CreateRoleAsync(string roleName, Guid? tenantId)
        {
            var systemAdminRole =
                await RoleRepository.FindByNormalizedNameAsync(LookupNormalizer.NormalizeName(roleName));

            if (systemAdminRole == null)
            {
                systemAdminRole = new IdentityRole(
                    GuidGenerator.Create(),
                    roleName,
                    tenantId
                )
                {
                    IsStatic = true,
                    IsPublic = true
                };

                (await RoleManager.CreateAsync(systemAdminRole)).CheckErrors();                
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Data;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Integration.Css;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Handlers
{
    public class TenantCreatedEventHandler
        : ILocalEventHandler<TenantCreatedEto>, ITransientDependency
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ICssUsersApiService _userCssApiService;
        private readonly IPersonRepository _personRepository;
        private readonly IIdentityUserRepository _identityUserRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly GrantManagerDbMigrationService _grantManagerDbMigrationService;

        public TenantCreatedEventHandler(ITenantRepository tenantRepository,
            GrantManagerDbMigrationService grantManagerDbMigrationService,
            ICssUsersApiService userCssApiService,
            IPersonRepository personRepository,
            IIdentityUserRepository identityUserRepository,
            ICurrentTenant currentTenant)
        {
            _tenantRepository = tenantRepository;
            _grantManagerDbMigrationService = grantManagerDbMigrationService;
            _userCssApiService = userCssApiService;
            _personRepository = personRepository;
            _identityUserRepository = identityUserRepository;
            _currentTenant = currentTenant;
        }

        public async Task HandleEventAsync(TenantCreatedEto tenantCreatedEto)
        {            
            var tenant = await _tenantRepository.GetAsync(tenantCreatedEto.Id);

            var adminIdentifier = tenantCreatedEto.Properties["AdminIdentifier"];
            var userSearchResult = await _userCssApiService.FindUserAsync("IDIR", adminIdentifier);

            if (userSearchResult == null ||
                !userSearchResult.Success ||
                userSearchResult.Data == null ||
                userSearchResult.Data?.Length == 0)
            {
                throw new UserFriendlyException("Error getting user details");
            }

            var user = userSearchResult!.Data![0];

            var identityUser = await AddBusinessAdminUserAsync(tenant.Id, user);
            
            await _grantManagerDbMigrationService
                .MigrateAndSeedTenantAsync(new HashSet<string>(), tenant);
            
            using (_currentTenant.Change(tenant.Id))
            {
                await SyncUserToTenantDatabaseAsync(identityUser, tenant.Id);
            }
        }

        private async Task<IdentityUser> AddBusinessAdminUserAsync(Guid tenantId, CssUser user)
        {
            var newUserId = Guid.NewGuid();
            var oidcSub = user.Username;
            var newUser = new IdentityUser(newUserId, user.Attributes.IdirUsername[0], user.Email ?? "blank@example.com", tenantId)
            {
                Name = user.FirstName,
                Surname = user.FirstName,
            };
            newUser.SetEmailConfirmed(true);

            var addedUser = await _identityUserRepository.InsertAsync(newUser, true);
            return await UpdateAdditionalUserPropertiesAsync(addedUser, oidcSub, user.Attributes.DisplayName[0]);
        }

        private async Task<IdentityUser> UpdateAdditionalUserPropertiesAsync(IdentityUser user,
            string oidcSub,
            string displayName)
        {
            if (user != null)
            {
                user.SetProperty("OidcSub", oidcSub);
                user.SetProperty("DisplayName", displayName);
                await _identityUserRepository.UpdateAsync(user, true);
            }

            return user!;
        }

        private async Task SyncUserToTenantDatabaseAsync(IdentityUser user, Guid tenantId)
        {
            var oidcSub = user.GetProperty("OidcSub")?.ToString();
            var displayName = user.GetProperty("DisplayName")?.ToString();

            // Create tenant level user
            if (oidcSub != null)
            {
                using (_currentTenant.Change(tenantId))
                {
                    var x = await _personRepository.GetListAsync();
                    var existingUser = await _personRepository.FindByOidcSub(oidcSub);

                    if (existingUser == null)
                    {
                        var person = await _personRepository.InsertAsync(new Person()
                        {
                            Id = user.Id,
                            OidcSub = oidcSub,
                            OidcDisplayName = displayName ?? string.Empty,
                            FullName = $"{user.Name} {user.Surname}",
                            Badge = Utils.CreateUserBadge(user)
                        });
                        await _personRepository.UpdateAsync(person, true);
                    }
                }
            }
        }
    }
}

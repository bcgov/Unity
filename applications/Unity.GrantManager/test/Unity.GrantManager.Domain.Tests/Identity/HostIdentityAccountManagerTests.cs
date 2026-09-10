using System;
using System.Threading.Tasks;
using Shouldly;
using Unity.Modules.Shared.Permissions;
using Xunit;

namespace Unity.GrantManager.Identity
{
    public class HostIdentityAccountManagerTests : GrantManagerDomainTestBase
    {
        private readonly HostIdentityAccountManager _manager;

        public HostIdentityAccountManagerTests()
        {
            _manager = GetRequiredService<HostIdentityAccountManager>();
        }

        [Fact]
        public async Task FindOrCreateHostAccountAsync_ShouldCreateNewHostAccount_WhenNoneExists()
        {
            var username = $"testuser{Guid.NewGuid():N}";
            var oidcSub = Guid.NewGuid().ToString();

            var user = await WithUnitOfWorkAsync(() => _manager.FindOrCreateHostAccountAsync(
                username, oidcSub, "First", "Last", "test@example.com", "Test User"));

            user.ShouldNotBeNull();
            user.TenantId.ShouldBeNull();
            user.UserName.ShouldBe(username);
        }

        [Fact]
        public async Task FindOrCreateHostAccountAsync_ShouldReturnExistingHostAccount_WhenCalledTwice()
        {
            var username = $"testuser{Guid.NewGuid():N}";
            var oidcSub = Guid.NewGuid().ToString();

            var firstUser = await WithUnitOfWorkAsync(() => _manager.FindOrCreateHostAccountAsync(
                username, oidcSub, "First", "Last", "test@example.com", "Test User"));

            var secondUser = await WithUnitOfWorkAsync(() => _manager.FindOrCreateHostAccountAsync(
                username, oidcSub, "First", "Last", "test@example.com", "Test User"));

            secondUser.Id.ShouldBe(firstUser.Id);
        }

        [Fact]
        public async Task AssignRoleClaimAsync_ShouldAllowBothRolesSimultaneously()
        {
            var username = $"testuser{Guid.NewGuid():N}";
            var oidcSub = Guid.NewGuid().ToString();

            var userId = (await WithUnitOfWorkAsync(() => _manager.FindOrCreateHostAccountAsync(
                username, oidcSub, "First", "Last", "test@example.com", "Test User"))).Id;

            // Each assignment re-fetches its own tracked instance within its own unit of work -
            // reusing an entity instance across separate units of work (as HostRoleAppService never
            // does; it does everything within one ambient UoW) trips EF Core's change tracker.
            await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                await _manager.AssignRoleClaimAsync(user, IdentityConsts.ITAdminRoleName);
            });
            await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                await _manager.AssignRoleClaimAsync(user, IdentityConsts.ITOperationsRoleName);
            });

            var roleNames = await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                return await _manager.GetRoleClaimsAsync(user);
            });

            roleNames.Count.ShouldBe(2);
            roleNames.ShouldContain(IdentityConsts.ITAdminRoleName);
            roleNames.ShouldContain(IdentityConsts.ITOperationsRoleName);
        }

        [Fact]
        public async Task AssignRoleClaimAsync_ShouldBeIdempotent_WhenRoleAlreadyAssigned()
        {
            var username = $"testuser{Guid.NewGuid():N}";
            var oidcSub = Guid.NewGuid().ToString();

            var userId = (await WithUnitOfWorkAsync(() => _manager.FindOrCreateHostAccountAsync(
                username, oidcSub, "First", "Last", "test@example.com", "Test User"))).Id;

            await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                await _manager.AssignRoleClaimAsync(user, IdentityConsts.ITAdminRoleName);
            });
            await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                await _manager.AssignRoleClaimAsync(user, IdentityConsts.ITAdminRoleName);
            });

            var roleNames = await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                return await _manager.GetRoleClaimsAsync(user);
            });

            roleNames.Count.ShouldBe(1);
        }

        [Fact]
        public async Task RevokeRoleClaimAsync_ShouldOnlyRemoveTheSpecifiedRole()
        {
            var username = $"testuser{Guid.NewGuid():N}";
            var oidcSub = Guid.NewGuid().ToString();

            var userId = (await WithUnitOfWorkAsync(() => _manager.FindOrCreateHostAccountAsync(
                username, oidcSub, "First", "Last", "test@example.com", "Test User"))).Id;

            await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                await _manager.AssignRoleClaimAsync(user, IdentityConsts.ITAdminRoleName);
            });
            await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                await _manager.AssignRoleClaimAsync(user, IdentityConsts.ITOperationsRoleName);
            });
            await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                await _manager.RevokeRoleClaimAsync(user, IdentityConsts.ITAdminRoleName);
            });

            var roleNames = await WithUnitOfWorkAsync(async () =>
            {
                var user = await _manager.GetHostAccountAsync(userId);
                return await _manager.GetRoleClaimsAsync(user);
            });

            roleNames.ShouldBe([IdentityConsts.ITOperationsRoleName]);
        }

        [Fact]
        public async Task GetHostAccountsAsync_ShouldOnlyReturnAccountsWithNullTenantId()
        {
            var username = $"testuser{Guid.NewGuid():N}";
            var oidcSub = Guid.NewGuid().ToString();

            var user = await WithUnitOfWorkAsync(() => _manager.FindOrCreateHostAccountAsync(
                username, oidcSub, "First", "Last", "test@example.com", "Test User"));

            var hostAccounts = await WithUnitOfWorkAsync(() => _manager.GetHostAccountsAsync());

            hostAccounts.ShouldContain(u => u.Id == user.Id);
            hostAccounts.ShouldAllBe(u => u.TenantId == null);
        }
    }
}

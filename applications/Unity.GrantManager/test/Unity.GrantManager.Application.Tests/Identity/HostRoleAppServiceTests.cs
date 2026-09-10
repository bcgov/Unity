using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Integrations.Css;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Validation;
using Xunit;

namespace Unity.GrantManager.Identity
{
    public class HostRoleAppServiceTests
    {
        private readonly ICssUsersApiService _cssUsersApiService;
        private readonly HostIdentityAccountManager _hostIdentityAccountManager;
        private readonly HostRoleAppService _appService;

        public HostRoleAppServiceTests()
        {
            _cssUsersApiService = Substitute.For<ICssUsersApiService>();
            _hostIdentityAccountManager = Substitute.For<HostIdentityAccountManager>(
                new object[] { null!, null!, null!, null!, null! });
            _appService = new HostRoleAppService(_cssUsersApiService, _hostIdentityAccountManager);
        }

        [Fact]
        public async Task AssignRoleAsync_ShouldThrow_WhenNoRoleNamesGiven()
        {
            var input = new AssignHostRoleDto { Directory = "IDIR", Guid = "abc", RoleNames = [] };

            await Should.ThrowAsync<AbpValidationException>(() => _appService.AssignRoleAsync(input));
            await _cssUsersApiService.DidNotReceive().FindUserAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task AssignRoleAsync_ShouldThrow_WhenRoleNameIsUnsupported()
        {
            var input = new AssignHostRoleDto { Directory = "IDIR", Guid = "abc", RoleNames = ["NotARole"] };

            await Should.ThrowAsync<AbpValidationException>(() => _appService.AssignRoleAsync(input));
            await _cssUsersApiService.DidNotReceive().FindUserAsync(Arg.Any<string>(), Arg.Any<string>());
        }

        [Fact]
        public async Task AssignRoleAsync_ShouldThrow_WhenDirectoryUserNotFound()
        {
            var input = new AssignHostRoleDto { Directory = "IDIR", Guid = "abc", RoleNames = [IdentityConsts.ITOperationsRoleName] };
            _cssUsersApiService.FindUserAsync(input.Directory, input.Guid).Returns(new UserSearchResult { Success = true, Data = [] });

            await Should.ThrowAsync<AbpValidationException>(() => _appService.AssignRoleAsync(input));
        }

        [Fact]
        public async Task AssignRoleAsync_ShouldFindOrCreateHostAccountAndAssignRoleClaim()
        {
            var input = new AssignHostRoleDto { Directory = "IDIR", Guid = "abc", RoleNames = [IdentityConsts.ITAdminRoleName] };
            var cssUser = new CssUser
            {
                FirstName = "First",
                LastName = "Last",
                Email = "first.last@example.com",
                Attributes = new CssUserAttributes
                {
                    IdirUserGuid = ["11111111-1111-1111-1111-111111111111"],
                    IdirUsername = ["FLAST"],
                    DisplayName = ["First Last"]
                }
            };
            _cssUsersApiService.FindUserAsync(input.Directory, input.Guid)
                .Returns(new UserSearchResult { Success = true, Data = [cssUser] });

            var hostAccount = new IdentityUser(Guid.NewGuid(), "FLAST", "first.last@example.com", null);
            _hostIdentityAccountManager.FindOrCreateHostAccountAsync(
                    "FLAST", Arg.Any<string>(), "First", "Last", "first.last@example.com", "First Last")
                .Returns(hostAccount);

            await _appService.AssignRoleAsync(input);

            await _hostIdentityAccountManager.Received(1).AssignRoleClaimAsync(hostAccount, IdentityConsts.ITAdminRoleName);
        }

        [Fact]
        public async Task AssignRoleAsync_ShouldAssignBothRoles_WhenBothRequestedInOneCall()
        {
            var input = new AssignHostRoleDto
            {
                Directory = "IDIR",
                Guid = "abc",
                RoleNames = [IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName]
            };
            var cssUser = new CssUser
            {
                FirstName = "First",
                LastName = "Last",
                Email = "first.last@example.com",
                Attributes = new CssUserAttributes
                {
                    IdirUserGuid = ["11111111-1111-1111-1111-111111111111"],
                    IdirUsername = ["FLAST"],
                    DisplayName = ["First Last"]
                }
            };
            _cssUsersApiService.FindUserAsync(input.Directory, input.Guid)
                .Returns(new UserSearchResult { Success = true, Data = [cssUser] });

            var hostAccount = new IdentityUser(Guid.NewGuid(), "FLAST", "first.last@example.com", null);
            _hostIdentityAccountManager.FindOrCreateHostAccountAsync(
                    "FLAST", Arg.Any<string>(), "First", "Last", "first.last@example.com", "First Last")
                .Returns(hostAccount);

            await _appService.AssignRoleAsync(input);

            await _hostIdentityAccountManager.Received(1).AssignRoleClaimAsync(hostAccount, IdentityConsts.ITAdminRoleName);
            await _hostIdentityAccountManager.Received(1).AssignRoleClaimAsync(hostAccount, IdentityConsts.ITOperationsRoleName);
            await _hostIdentityAccountManager.Received(1).FindOrCreateHostAccountAsync(
                "FLAST", Arg.Any<string>(), "First", "Last", "first.last@example.com", "First Last");
        }

        [Fact]
        public async Task RevokeRoleAsync_ShouldLookUpHostAccountAndRevokeOnlyTheSpecifiedRoleClaim()
        {
            var hostAccount = new IdentityUser(Guid.NewGuid(), "FLAST", "first.last@example.com", null);
            _hostIdentityAccountManager.GetHostAccountAsync(hostAccount.Id).Returns(hostAccount);

            await _appService.RevokeRoleAsync(hostAccount.Id, IdentityConsts.ITAdminRoleName);

            await _hostIdentityAccountManager.Received(1).RevokeRoleClaimAsync(hostAccount, IdentityConsts.ITAdminRoleName);
        }

        [Fact]
        public async Task GetListAsync_ShouldReturnOneRowPerUser_CarryingAllTheirRoles()
        {
            var withBothRoles = new IdentityUser(Guid.NewGuid(), "bothroles", "bothroles@example.com", null);
            withBothRoles.SetProperty("DisplayName", "Both Roles");
            var withoutRole = new IdentityUser(Guid.NewGuid(), "withoutrole", "withoutrole@example.com", null);

            _hostIdentityAccountManager.GetHostAccountsAsync().Returns(new System.Collections.Generic.List<IdentityUser> { withBothRoles, withoutRole });
            _hostIdentityAccountManager.GetRoleClaimsAsync(withBothRoles)
                .Returns(new[] { IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName });
            _hostIdentityAccountManager.GetRoleClaimsAsync(withoutRole).Returns(System.Array.Empty<string>());

            var result = await _appService.GetListAsync();

            // withoutRole holds no IT roles, so it's omitted entirely rather than appearing as an
            // empty-roles row.
            result.Count.ShouldBe(1);
            result[0].Id.ShouldBe(withBothRoles.Id);
            result[0].RoleNames.ShouldBe([IdentityConsts.ITAdminRoleName, IdentityConsts.ITOperationsRoleName], ignoreOrder: true);
        }
    }
}

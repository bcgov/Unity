using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Web.Identity.Authorization;
using Volo.Abp.Users;
using Xunit;

namespace Unity.GrantManager.Identity.Authorization;

public class RoleClaimAuthorizationHandlerTests
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserClaimsRoleChecker _userClaimsRoleChecker;
    private readonly RoleClaimAuthorizationHandler _handler;

    public RoleClaimAuthorizationHandlerTests()
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _userClaimsRoleChecker = Substitute.For<IUserClaimsRoleChecker>();
        _userClaimsRoleChecker
            .HasAnyRoleAsync(Arg.Any<Guid?>(), Arg.Any<IEnumerable<string>>())
            .Returns(false);
        _handler = new RoleClaimAuthorizationHandler(_currentUser, _userClaimsRoleChecker);
    }

    private static AuthorizationHandlerContext CreateContext(
        ClaimsPrincipal user,
        RoleClaimRequirement requirement)
    {
        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }

    private static ClaimsPrincipal CreateUserWithRole(string roleName)
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUserWithoutRole()
    {
        var identity = new ClaimsIdentity("TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasTokenRoleClaim()
    {
        var user = CreateUserWithRole("ITAdministrator");
        var requirement = new RoleClaimRequirement(["ITAdministrator"]);
        var context = CreateContext(user, requirement);

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
        await _userClaimsRoleChecker.DidNotReceive().HasAnyRoleAsync(Arg.Any<Guid?>(), Arg.Any<IEnumerable<string>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasMatchingClaimInUserClaimsTable()
    {
        var userId = Guid.NewGuid();
        var user = CreateUserWithoutRole();
        var requirement = new RoleClaimRequirement(["ITAdministrator"]);

        _currentUser.Id.Returns(userId);
        _userClaimsRoleChecker.HasAnyRoleAsync(userId, requirement.RoleNames).Returns(true);

        var context = CreateContext(user, requirement);

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSucceed_WhenNeitherTokenRoleNorUserClaimsTableMatch()
    {
        var userId = Guid.NewGuid();
        var user = CreateUserWithoutRole();
        var requirement = new RoleClaimRequirement(["ITAdministrator"]);

        _currentUser.Id.Returns(userId);
        _userClaimsRoleChecker.HasAnyRoleAsync(userId, requirement.RoleNames).Returns(false);

        var context = CreateContext(user, requirement);

        await _handler.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void Requirement_ShouldStoreRoles()
    {
        var requirement = new RoleClaimRequirement(["ITAdministrator", "ITOperations"]);
        requirement.RoleNames.ShouldBe(["ITAdministrator", "ITOperations"]);
    }
}

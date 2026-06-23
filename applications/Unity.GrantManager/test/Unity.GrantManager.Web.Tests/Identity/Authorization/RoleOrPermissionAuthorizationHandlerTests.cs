using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Web.Identity.Authorization;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace Unity.GrantManager.Identity.Authorization;

public class RoleOrPermissionAuthorizationHandlerTests
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly RoleOrPermissionAuthorizationHandler _handler;

    public RoleOrPermissionAuthorizationHandlerTests()
    {
        _permissionChecker = Substitute.For<IPermissionChecker>();
        _handler = new RoleOrPermissionAuthorizationHandler(_permissionChecker);
    }

    private static AuthorizationHandlerContext CreateContext(
        ClaimsPrincipal user,
        RoleOrPermissionRequirement requirement)
    {
        return new AuthorizationHandlerContext(
            [requirement],
            user,
            resource: null);
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
    public async Task HandleAsync_ShouldSucceed_WhenUserHasRole()
    {
        // Arrange
        var user = CreateUserWithRole("ITAdministrator");
        var requirement = new RoleOrPermissionRequirement("ITAdministrator", "Unity.ITAdmin");
        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
        await _permissionChecker.DidNotReceive().IsGrantedAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserHasPermission()
    {
        // Arrange
        var user = CreateUserWithoutRole();
        var requirement = new RoleOrPermissionRequirement("ITAdministrator", "Unity.ITAdmin");

        _permissionChecker.IsGrantedAsync(user, "Unity.ITAdmin")
            .Returns(true);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSucceed_WhenNeitherRoleNorPermission()
    {
        // Arrange
        var user = CreateUserWithoutRole();
        var requirement = new RoleOrPermissionRequirement("ITAdministrator", "Unity.ITAdmin");

        _permissionChecker.IsGrantedAsync(user, "Unity.ITAdmin")
            .Returns(false);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldShortCircuit_WhenRoleMatches()
    {
        // Arrange
        var user = CreateUserWithRole("ITOperations");
        var requirement = new RoleOrPermissionRequirement("ITOperations", "Unity.ITOperations");
        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert - should not call permission checker at all
        context.HasSucceeded.ShouldBeTrue();
        await _permissionChecker.DidNotReceive().IsGrantedAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_ShouldCheckPermission_WhenRoleDoesNotMatch()
    {
        // Arrange
        var user = CreateUserWithRole("SomeOtherRole");
        var requirement = new RoleOrPermissionRequirement("ITAdministrator", "Unity.ITAdmin");

        _permissionChecker.IsGrantedAsync(user, "Unity.ITAdmin")
            .Returns(false);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
        await _permissionChecker.Received(1).IsGrantedAsync(user, "Unity.ITAdmin");
    }

    [Fact]
    public void Requirement_ShouldStoreRoleAndPermission()
    {
        var requirement = new RoleOrPermissionRequirement("MyRole", "MyPermission");
        requirement.RoleName.ShouldBe("MyRole");
        requirement.PermissionName.ShouldBe("MyPermission");
    }
}

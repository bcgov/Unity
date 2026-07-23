using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Web.Identity.Authorization;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace Unity.GrantManager.Identity.Authorization;

public class PermissionOrAuthorizationHandlerTests
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly PermissionOrAuthorizationHandler _handler;

    public PermissionOrAuthorizationHandlerTests()
    {
        _permissionChecker = Substitute.For<IPermissionChecker>();
        _handler = new PermissionOrAuthorizationHandler(_permissionChecker);
    }

    private static AuthorizationHandlerContext CreateContext(
        ClaimsPrincipal user,
        PermissionOrRequirement requirement)
    {
        return new AuthorizationHandlerContext(
            [requirement],
            user,
            resource: null);
    }

    private static ClaimsPrincipal CreateUser()
    {
        var identity = new ClaimsIdentity("TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenAnyPermissionIsGranted()
    {
        // Arrange
        var user = CreateUser();
        var permissions = new[] { "Perm.A", "Perm.B", "Perm.C" };
        var requirement = new PermissionOrRequirement(permissions);

        var grantResult = new MultiplePermissionGrantResult();
        grantResult.Result.Add("Perm.A", PermissionGrantResult.Prohibited);
        grantResult.Result.Add("Perm.B", PermissionGrantResult.Granted);
        grantResult.Result.Add("Perm.C", PermissionGrantResult.Prohibited);

        _permissionChecker.IsGrantedAsync(user, permissions)
            .Returns(grantResult);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSucceed_WhenNoPermissionsGranted()
    {
        // Arrange
        var user = CreateUser();
        var permissions = new[] { "Perm.A", "Perm.B" };
        var requirement = new PermissionOrRequirement(permissions);

        var grantResult = new MultiplePermissionGrantResult();
        grantResult.Result.Add("Perm.A", PermissionGrantResult.Prohibited);
        grantResult.Result.Add("Perm.B", PermissionGrantResult.Prohibited);

        _permissionChecker.IsGrantedAsync(user, permissions)
            .Returns(grantResult);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenAllPermissionsGranted()
    {
        // Arrange
        var user = CreateUser();
        var permissions = new[] { "Perm.A", "Perm.B" };
        var requirement = new PermissionOrRequirement(permissions);

        var grantResult = new MultiplePermissionGrantResult();
        grantResult.Result.Add("Perm.A", PermissionGrantResult.Granted);
        grantResult.Result.Add("Perm.B", PermissionGrantResult.Granted);

        _permissionChecker.IsGrantedAsync(user, permissions)
            .Returns(grantResult);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldUseBatchApi_NotIndividualCalls()
    {
        // Arrange
        var user = CreateUser();
        var permissions = new[] { "Perm.X", "Perm.Y", "Perm.Z" };
        var requirement = new PermissionOrRequirement(permissions);

        var grantResult = new MultiplePermissionGrantResult();
        grantResult.Result.Add("Perm.X", PermissionGrantResult.Prohibited);
        grantResult.Result.Add("Perm.Y", PermissionGrantResult.Prohibited);
        grantResult.Result.Add("Perm.Z", PermissionGrantResult.Prohibited);

        _permissionChecker.IsGrantedAsync(user, permissions)
            .Returns(grantResult);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert - single batch call, not individual calls
        await _permissionChecker.Received(1).IsGrantedAsync(user, permissions);
        await _permissionChecker.DidNotReceive().IsGrantedAsync(user, Arg.Any<string>());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSucceed_WhenResultIsEmpty()
    {
        // Arrange
        var user = CreateUser();
        var permissions = new[] { "Perm.A" };
        var requirement = new PermissionOrRequirement(permissions);

        var grantResult = new MultiplePermissionGrantResult();

        _permissionChecker.IsGrantedAsync(user, permissions)
            .Returns(grantResult);

        var context = CreateContext(user, requirement);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public void Requirement_ShouldStorePermissions()
    {
        var requirement = new PermissionOrRequirement("A", "B", "C");
        requirement.Permissions.ShouldBe(new[] { "A", "B", "C" });
    }
}

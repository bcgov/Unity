using System;
using System.Security.Claims;
using Shouldly;
using Unity.GrantManager.Assessments;
using Volo.Abp.Security.Claims;
using Xunit;

namespace Unity.GrantManager.Assessments;

public class FindUserIdirIdTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaims(claims);
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void ShouldReturnGuid_WhenAbpUserIdClaimPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(
            new Claim(AbpClaimTypes.UserId, userId.ToString()));

        // Act
        var result = AssessmentAuthorizationHandler.FindUserIdirId(principal);

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void ShouldReturnGuid_WhenLegacyUserIdClaimPresent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var principal = CreatePrincipal(
            new Claim("UserId", userId.ToString()));

        // Act
        var result = AssessmentAuthorizationHandler.FindUserIdirId(principal);

        // Assert
        result.ShouldBe(userId);
    }

    [Fact]
    public void ShouldPreferAbpClaim_WhenBothPresent()
    {
        // Arrange
        var abpUserId = Guid.NewGuid();
        var legacyUserId = Guid.NewGuid();
        var principal = CreatePrincipal(
            new Claim(AbpClaimTypes.UserId, abpUserId.ToString()),
            new Claim("UserId", legacyUserId.ToString()));

        // Act
        var result = AssessmentAuthorizationHandler.FindUserIdirId(principal);

        // Assert
        result.ShouldBe(abpUserId);
    }

    [Fact]
    public void ShouldFallbackToLegacy_WhenAbpClaimMissing()
    {
        // Arrange
        var legacyUserId = Guid.NewGuid();
        var principal = CreatePrincipal(
            new Claim("DisplayName", "Test User"),
            new Claim("UserId", legacyUserId.ToString()));

        // Act
        var result = AssessmentAuthorizationHandler.FindUserIdirId(principal);

        // Assert
        result.ShouldBe(legacyUserId);
    }

    [Fact]
    public void ShouldReturnNull_WhenNeitherClaimPresent()
    {
        // Arrange
        var principal = CreatePrincipal(
            new Claim("DisplayName", "Test User"));

        // Act
        var result = AssessmentAuthorizationHandler.FindUserIdirId(principal);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ShouldReturnNull_WhenClaimValueIsEmpty()
    {
        // Arrange
        var principal = CreatePrincipal(
            new Claim(AbpClaimTypes.UserId, ""),
            new Claim("UserId", ""));

        // Act
        var result = AssessmentAuthorizationHandler.FindUserIdirId(principal);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ShouldReturnNull_WhenClaimValueIsNotValidGuid()
    {
        // Arrange
        var principal = CreatePrincipal(
            new Claim(AbpClaimTypes.UserId, "not-a-guid"));

        // Act
        var result = AssessmentAuthorizationHandler.FindUserIdirId(principal);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ShouldThrow_WhenPrincipalIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            AssessmentAuthorizationHandler.FindUserIdirId(null!));
    }
}

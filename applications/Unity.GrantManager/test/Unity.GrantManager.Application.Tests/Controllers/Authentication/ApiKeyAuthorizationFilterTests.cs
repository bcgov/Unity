using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Unity.GrantManager.ApplicationForms;
using Xunit;

namespace Unity.GrantManager.Controllers.Authentication;

public class ApiKeyAuthorizationFilterTests
{
    [Fact]
    [Trait("Category", "Security")]
    public void OnAuthorization_401_WhenConfiguredKeyIsWhitespace_EvenIfHeaderMatchesExactly()
    {
        // Arrange - CWE-287: B2BAuth:ApiKey configured as whitespace-only ("not really set")
        // and an attacker sends that same whitespace value as the header.
        var filter = BuildFilter(configuredApiKey: " ");
        var context = BuildContext(new Dictionary<string, string>
        {
            [AuthConstants.ApiKeyHeader] = " "
        });

        // Act
        filter.OnAuthorization(context);

        // Assert - must be rejected; a whitespace-only configuration is not a real API key.
        context.Result.ShouldBeOfType<UnauthorizedObjectResult>();
        var result = (UnauthorizedObjectResult)context.Result!;
        ((ProblemDetails)result.Value!).Detail.ShouldBe("API Key not configured");
    }

    [Fact]
    [Trait("Category", "Security")]
    public void OnAuthorization_401_WhenConfiguredKeyIsEmpty()
    {
        // Arrange - CWE-287: B2BAuth:ApiKey configured as an empty string.
        // The header must carry a non-empty value so the request reaches the
        // configured-key check instead of short-circuiting on "API Key missing".
        var filter = BuildFilter(configuredApiKey: string.Empty);
        var context = BuildContext(new Dictionary<string, string>
        {
            [AuthConstants.ApiKeyHeader] = "some-supplied-value"
        });

        // Act
        filter.OnAuthorization(context);

        // Assert
        context.Result.ShouldBeOfType<UnauthorizedObjectResult>();
        var result = (UnauthorizedObjectResult)context.Result!;
        ((ProblemDetails)result.Value!).Detail.ShouldBe("API Key not configured");
    }

    [Fact]
    [Trait("Category", "Security")]
    public void OnAuthorization_401_WhenHeaderIsMissing()
    {
        // Arrange - no B2BAuth header supplied at all.
        var filter = BuildFilter(configuredApiKey: "correct-key");
        var context = BuildContext(new Dictionary<string, string>());

        // Act
        filter.OnAuthorization(context);

        // Assert
        context.Result.ShouldBeOfType<UnauthorizedObjectResult>();
        var result = (UnauthorizedObjectResult)context.Result!;
        ((ProblemDetails)result.Value!).Detail.ShouldBe("API Key missing");
    }

    [Fact]
    [Trait("Category", "Security")]
    public void OnAuthorization_401_WhenHeaderDoesNotMatchConfiguredKey()
    {
        // Arrange - header supplied but does not match the configured key.
        var filter = BuildFilter(configuredApiKey: "correct-key");
        var context = BuildContext(new Dictionary<string, string>
        {
            [AuthConstants.ApiKeyHeader] = "wrong-key"
        });

        // Act
        filter.OnAuthorization(context);

        // Assert
        context.Result.ShouldBeOfType<UnauthorizedObjectResult>();
        var result = (UnauthorizedObjectResult)context.Result!;
        ((ProblemDetails)result.Value!).Detail.ShouldBe("Invalid API Key");
    }

    [Fact]
    [Trait("Category", "Security")]
    public void OnAuthorization_Allows_WhenHeaderMatchesConfiguredKey()
    {
        // Arrange - header exactly matches the configured key.
        var filter = BuildFilter(configuredApiKey: "correct-key");
        var context = BuildContext(new Dictionary<string, string>
        {
            [AuthConstants.ApiKeyHeader] = "correct-key"
        });

        // Act
        filter.OnAuthorization(context);

        // Assert - no result means the request is allowed to proceed.
        context.Result.ShouldBeNull();
    }

    private static ApiKeyAuthorizationFilter BuildFilter(string configuredApiKey)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["B2BAuth:ApiKey"] = configuredApiKey
            }).Build();

        return new ApiKeyAuthorizationFilter(configuration);
    }

    private static AuthorizationFilterContext BuildContext(Dictionary<string, string> headers)
    {
        var httpContext = new DefaultHttpContext();

        foreach (var kvp in headers)
        {
            httpContext.Request.Headers.Append(kvp.Key, kvp.Value);
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }
}

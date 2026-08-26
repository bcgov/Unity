using Shouldly;
using Unity.GrantManager.Integrations;
using Xunit;

namespace Unity.GrantManager.Domain.Tests.Integrations;

public class DynamicUrlDataSeederTests
{
    private const string DevUrl = "https://dev-example.gov.bc.ca";
    private const string TestUrl = "https://test-example.gov.bc.ca";
    private const string ProdUrl = "https://prod-example.gov.bc.ca";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Development")]
    [InlineData("dev2")]
    public void GetEnvironmentUrl_DevOrUnset_ReturnsDevUrl(string? aspNetCoreEnvironment)
    {
        var result = DynamicUrlDataSeeder.GetEnvironmentUrl(
            aspNetCoreEnvironment, DevUrl, TestUrl, ProdUrl);

        result.ShouldBe(DevUrl);
    }

    [Theory]
    [InlineData("Test")]
    [InlineData("test2")]
    [InlineData("UAT")]
    public void GetEnvironmentUrl_TestOrUat_ReturnsTestUrl(string aspNetCoreEnvironment)
    {
        var result = DynamicUrlDataSeeder.GetEnvironmentUrl(
            aspNetCoreEnvironment, DevUrl, TestUrl, ProdUrl);

        result.ShouldBe(TestUrl);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void GetEnvironmentUrl_AnythingElse_ReturnsProdUrl(string aspNetCoreEnvironment)
    {
        var result = DynamicUrlDataSeeder.GetEnvironmentUrl(
            aspNetCoreEnvironment, DevUrl, TestUrl, ProdUrl);

        result.ShouldBe(ProdUrl);
    }
}

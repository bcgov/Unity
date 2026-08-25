using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace Unity.GrantManager.Handlers;

public class TenantCreatedEventHandlerTests
{
    [Fact]
    public async Task ResolveMetabaseUserEmailsAsync_PropertyOmitted_SnapshotsCurrentGlobalDefault()
    {
        var etoProperties = new Dictionary<string, string>();

        var result = await TenantCreatedEventHandler.ResolveMetabaseUserEmailsAsync(
            etoProperties, () => Task.FromResult<string?>("global1@gov.bc.ca,global2@gov.bc.ca"));

        result.ShouldBe("global1@gov.bc.ca,global2@gov.bc.ca");
    }

    [Fact]
    public async Task ResolveMetabaseUserEmailsAsync_PropertyOmittedAndNoGlobalDefaultSet_ReturnsEmpty()
    {
        var etoProperties = new Dictionary<string, string>();

        var result = await TenantCreatedEventHandler.ResolveMetabaseUserEmailsAsync(
            etoProperties, () => Task.FromResult<string?>(null));

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task ResolveMetabaseUserEmailsAsync_PropertyExplicitlyEmpty_ReturnsEmptyWithoutFallingBackToGlobal()
    {
        var etoProperties = new Dictionary<string, string> { ["MetabaseUserEmails"] = string.Empty };
        var globalDefaultLookedUp = false;

        var result = await TenantCreatedEventHandler.ResolveMetabaseUserEmailsAsync(etoProperties, () =>
        {
            globalDefaultLookedUp = true;
            return Task.FromResult<string?>("should-not-be-used@gov.bc.ca");
        });

        result.ShouldBe(string.Empty);
        globalDefaultLookedUp.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveMetabaseUserEmailsAsync_PropertyExplicitlySet_ReturnsThatValueWithoutFallingBackToGlobal()
    {
        var etoProperties = new Dictionary<string, string> { ["MetabaseUserEmails"] = "tenant-specific@gov.bc.ca" };
        var globalDefaultLookedUp = false;

        var result = await TenantCreatedEventHandler.ResolveMetabaseUserEmailsAsync(etoProperties, () =>
        {
            globalDefaultLookedUp = true;
            return Task.FromResult<string?>("should-not-be-used@gov.bc.ca");
        });

        result.ShouldBe("tenant-specific@gov.bc.ca");
        globalDefaultLookedUp.ShouldBeFalse();
    }

    [Fact]
    public void BuildFeatureUpdates_NullInput_ReturnsEmpty()
    {
        var result = TenantCreatedEventHandler.BuildFeatureUpdates(null);

        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildFeatureUpdates_BlankInput_ReturnsEmpty(string featureKeysRaw)
    {
        var result = TenantCreatedEventHandler.BuildFeatureUpdates(featureKeysRaw);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void BuildFeatureUpdates_CommaSeparatedKeys_ReturnsOneEnabledUpdatePerKey()
    {
        var result = TenantCreatedEventHandler.BuildFeatureUpdates("Unity.Payments, Unity.Reporting ,Unity.Notifications");

        result.Count.ShouldBe(3);
        result.ShouldAllBe(f => f.Value == "True");
        result.ShouldContain(f => f.Name == "Unity.Payments");
        result.ShouldContain(f => f.Name == "Unity.Reporting");
        result.ShouldContain(f => f.Name == "Unity.Notifications");
    }

    [Fact]
    public void BuildFeatureUpdates_EmptyEntriesBetweenDelimiters_AreIgnored()
    {
        var result = TenantCreatedEventHandler.BuildFeatureUpdates("Unity.Payments,,  ,Unity.Reporting");

        result.Count.ShouldBe(2);
    }
}

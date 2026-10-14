using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace Unity.GrantManager.Handlers;

public class TenantCreatedEventHandlerTests
{
    [Fact]
    public void ResolveMetabaseUserEmails_PropertyOmitted_ReturnsEmpty()
    {
        var etoProperties = new Dictionary<string, string>();

        TenantCreatedEventHandler.ResolveMetabaseUserEmails(etoProperties).ShouldBe(string.Empty);
    }

    [Fact]
    public void ResolveMetabaseUserEmails_PropertyExplicitlyEmpty_ReturnsEmpty()
    {
        var etoProperties = new Dictionary<string, string> { ["MetabaseUserEmails"] = string.Empty };

        TenantCreatedEventHandler.ResolveMetabaseUserEmails(etoProperties).ShouldBe(string.Empty);
    }

    [Fact]
    public void ResolveMetabaseUserEmails_PropertyExplicitlySet_ReturnsThatValue()
    {
        var etoProperties = new Dictionary<string, string> { ["MetabaseUserEmails"] = "tenant-specific@gov.bc.ca" };

        TenantCreatedEventHandler.ResolveMetabaseUserEmails(etoProperties).ShouldBe("tenant-specific@gov.bc.ca");
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

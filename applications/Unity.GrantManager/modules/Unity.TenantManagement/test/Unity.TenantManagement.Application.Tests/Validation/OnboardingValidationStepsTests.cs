using System;
using System.Reflection;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Unity.TenantManagement;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Unity.TenantManagement.Validation;

public class SuperUsersValidationStepTests
{
    private static OnboardingRequestDto RequestWithSuperUsers(string superUsers) =>
        new() { SuperUsers = superUsers };

    [Fact]
    public async Task ValidateAsync_NoEmailsParsed_ReturnsFailure()
    {
        var lookup = Substitute.For<IOnboardingUserLookup>();
        var step = new SuperUsersValidationStep(lookup);

        var result = await step.ValidateAsync(RequestWithSuperUsers("not an email"));

        result.IsValid.ShouldBeFalse();
        await lookup.DidNotReceive().FindUserGuidByEmailAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task ValidateAsync_AnyParsedEmailResolves_ReturnsSuccess()
    {
        var lookup = Substitute.For<IOnboardingUserLookup>();
        lookup.FindUserGuidByEmailAsync("first@example.com").Returns((string)null);
        lookup.FindUserGuidByEmailAsync("second@example.com").Returns("guid-123");
        var step = new SuperUsersValidationStep(lookup);

        var result = await step.ValidateAsync(RequestWithSuperUsers("first@example.com; second@example.com"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_NoneOfTheParsedEmailsResolve_ReturnsFailure()
    {
        var lookup = Substitute.For<IOnboardingUserLookup>();
        lookup.FindUserGuidByEmailAsync(Arg.Any<string>()).Returns((string)null);
        var step = new SuperUsersValidationStep(lookup);

        var result = await step.ValidateAsync(RequestWithSuperUsers("first@example.com,second@example.com"));

        result.IsValid.ShouldBeFalse();
        result.Issue.ShouldNotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("a@example.com,b@example.com", new[] { "a@example.com", "b@example.com" })]
    [InlineData("a@example.com;b@example.com", new[] { "a@example.com", "b@example.com" })]
    [InlineData("a@example.com|b@example.com", new[] { "a@example.com", "b@example.com" })]
    [InlineData(" a@example.com , not-an-email , b@example.com ", new[] { "a@example.com", "b@example.com" })]
    public void ParseEmails_HandlesDelimitersAndDropsNonEmailTokens(string input, string[] expected)
    {
        var result = SuperUsersValidationStep.ParseEmails(input);

        result.ShouldBe(expected);
    }

    [Fact]
    public void ParseEmails_ExtractsEmailsFromFormioDataGridJson()
    {
        const string dataGridJson = """
        {
          "rows": [
            {
              "cells": [
                { "key": "s03_SuperUserName", "value": "Kingsley Shacklebolt" },
                { "key": "s03_SuperUserEmail", "value": "kingsley.shacklebolt@gov.bc.ca" },
                { "key": "s03_SuperUserTitle", "value": "Minister for Magic" }
              ]
            },
            {
              "cells": [
                { "key": "s03_SuperUserName", "value": "Minerva McGonagall" },
                { "key": "s03_SuperUserEmail", "value": "m.mcgonagall@hogwarts.ac.uk" },
                { "key": "s03_SuperUserTitle", "value": "External Liaison Officer" }
              ]
            }
          ]
        }
        """;

        var result = SuperUsersValidationStep.ParseEmails(dataGridJson);

        result.ShouldBe(["kingsley.shacklebolt@gov.bc.ca", "m.mcgonagall@hogwarts.ac.uk"]);
    }

    [Fact]
    public void ParseEmails_DataGridRowsWithoutEmailColumn_ReturnsEmpty()
    {
        const string dataGridJson = """
        {
          "rows": [
            { "cells": [ { "key": "s03_SuperUserName", "value": "Kingsley Shacklebolt" } ] }
          ]
        }
        """;

        var result = SuperUsersValidationStep.ParseEmails(dataGridJson);

        result.ShouldBeEmpty();
    }
}

public class TenantNameUniquenessStepTests
{
    private static OnboardingRequestDto RequestWithTenantName(string tenantName) =>
        new() { TenantName = tenantName };

    // Tenant's (Guid, string, string) constructor is internal to the ABP assembly; reflection is the
    // only way to build a real instance here, since this test only needs a non-null "found" result.
    private static Tenant NewTenant(string name) =>
        (Tenant)typeof(Tenant)
            .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(Guid), typeof(string), typeof(string)])!
            .Invoke([Guid.NewGuid(), name, name.ToUpperInvariant()]);

    [Fact]
    public async Task ValidateAsync_BlankTenantName_ReturnsFailureWithoutQueryingRepository()
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        var step = new TenantNameUniquenessStep(tenantRepository);

        var result = await step.ValidateAsync(RequestWithTenantName("   "));

        result.IsValid.ShouldBeFalse();
        await tenantRepository.DidNotReceive().FindByNameAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_NameAlreadyExists_ReturnsFailure()
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        var existing = NewTenant("Acme");
        tenantRepository.FindByNameAsync("ACME", Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(existing);
        var step = new TenantNameUniquenessStep(tenantRepository);

        var result = await step.ValidateAsync(RequestWithTenantName("Acme"));

        result.IsValid.ShouldBeFalse();
        result.Issue.ShouldContain("Acme");
    }

    [Fact]
    public async Task ValidateAsync_NameIsUnique_ReturnsSuccess()
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.FindByNameAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns((Tenant)null);
        var step = new TenantNameUniquenessStep(tenantRepository);

        var result = await step.ValidateAsync(RequestWithTenantName("Brand New Co"));

        result.IsValid.ShouldBeTrue();
    }
}

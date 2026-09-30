using System;
using Shouldly;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Xunit;

namespace Unity.GrantManager.Notifications;

public class ApplicantEmailRecipientResolverTests
{
    [Fact]
    public void Should_UseTheDisplayedPrimaryIncludingAnInferredPrimary()
    {
        var contacts = new[]
        {
            new ContactInfoItemDto { Email = "other@example.test" },
            new ContactInfoItemDto { IsPrimary = true, IsPrimaryInferred = true, Email = "primary@example.test", ContactType = "Application" }
        };
        ApplicantEmailRecipientResolver.Resolve("ApplicationContact", contacts).ShouldBe("primary@example.test");
    }

    [Theory]
    [InlineData("SigningAuthority")]
    [InlineData("SIGNING_AUTHORITY")]
    [InlineData("ADDITIONAL_SIGNING_AUTHORITY")]
    [InlineData("Additional Signing Authority")]
    public void Should_ResolveSigningAuthorityRoleVariants(string role)
    {
        var contacts = new[]
        {
            new ContactInfoItemDto { Email = "signer@example.test", Role = role },
            new ContactInfoItemDto { Email = "other@example.test", Role = "General" }
        };
        ApplicantEmailRecipientResolver.Resolve("SigningAuthority", contacts).ShouldBe("signer@example.test");
    }

    [Fact]
    public void Should_DeduplicateContactsAndExplicitAddresses()
    {
        var contacts = new[]
        {
            new ContactInfoItemDto { IsPrimary = true, Email = " signer@example.test ", Role = "SigningAuthority" },
            new ContactInfoItemDto { Email = "SIGNER@example.test", Role = "Additional Signing Authority" },
            new ContactInfoItemDto { Email = "second@example.test", Role = "SigningAuthority" }
        };
        ApplicantEmailRecipientResolver.Resolve("ApplicationContact,SigningAuthority,signer@example.test", contacts)
            .ShouldBe("signer@example.test; second@example.test");
    }

    [Fact]
    public void Should_LeaveUnresolvedRecipientsBlank()
    {
        ApplicantEmailRecipientResolver.Resolve("ApplicationContact,SigningAuthority", Array.Empty<ContactInfoItemDto>()).ShouldBeEmpty();
        ApplicantEmailRecipientResolver.Resolve("ApplicationContact", [new ContactInfoItemDto { IsPrimary = true, Email = "invalid" }]).ShouldBeEmpty();
    }
}

using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Integrations.Cas;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.Intakes;

/// <summary>
/// The CHEFS applicant lookup must return the same primary physical and mailing addresses as the
/// applicant details page and the applicant portal, regardless of the order the database returns rows in.
/// </summary>
public class ApplicantLookupServiceTests
{
    private const string UnityApplicantId = "100002";

    private static readonly DateTime Older = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Newer = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Applicant _applicant;
    private readonly IApplicantAddressRepository _addressRepository = Substitute.For<IApplicantAddressRepository>();
    private readonly ApplicantLookupService _service;

    public ApplicantLookupServiceTests()
    {
        _applicant = new Applicant { ApplicantName = "Test Applicant", UnityApplicantId = UnityApplicantId };
        EntityHelper.TrySetId(_applicant, Guid.NewGuid);

        var applicantRepository = Substitute.For<IApplicantRepository>();
        applicantRepository.GetByUnityApplicantIdAsync(UnityApplicantId).Returns(_applicant);

        _service = new ApplicantLookupService(
            Substitute.For<ISupplierService>(),
            Substitute.For<IApplicantAppService>(),
            applicantRepository,
            _addressRepository,
            Substitute.For<IApplicantAgentRepository>());
    }

    private static ApplicantAddress Address(
        AddressType addressType,
        string street,
        bool isPrimary,
        DateTime creationTime,
        DateTime? lastModificationTime = null)
    {
        var address = new ApplicantAddress
        {
            AddressType = addressType,
            Street = street,
            CreationTime = creationTime,
            LastModificationTime = lastModificationTime
        };
        EntityHelper.TrySetId(address, Guid.NewGuid);
        address.SetPrimaryFlag(isPrimary);
        return address;
    }

    private void GivenAddresses(params ApplicantAddress[] addresses)
    {
        _addressRepository.FindByApplicantIdAsync(_applicant.Id).Returns(new List<ApplicantAddress>(addresses));
    }

    private async Task<JObject> LookupAsync()
        => JObject.Parse(await _service.ApplicantLookupByApplicantId(UnityApplicantId));

    [Fact]
    public async Task Should_ReturnFlaggedPrimary_WhenItIsNotTheFirstRowOfItsType()
    {
        GivenAddresses(
            Address(AddressType.PhysicalAddress, "Newer Physical", isPrimary: false, Newer),
            Address(AddressType.MailingAddress, "Newer Mailing", isPrimary: false, Newer),
            Address(AddressType.PhysicalAddress, "Primary Physical", isPrimary: true, Older),
            Address(AddressType.MailingAddress, "Primary Mailing", isPrimary: true, Older));

        var result = await LookupAsync();

        result["PhysicalAddressLine1"]!.Value<string>().ShouldBe("Primary Physical");
        result["MailingAddressLine1"]!.Value<string>().ShouldBe("Primary Mailing");
    }

    [Fact]
    public async Task Should_ReturnNewestByCreationTime_WhenNoAddressIsFlagged()
    {
        GivenAddresses(
            Address(AddressType.PhysicalAddress, "Older Physical", isPrimary: false, Older),
            Address(AddressType.MailingAddress, "Older Mailing", isPrimary: false, Older),
            Address(AddressType.PhysicalAddress, "Newer Physical", isPrimary: false, Newer),
            Address(AddressType.MailingAddress, "Newer Mailing", isPrimary: false, Newer));

        var result = await LookupAsync();

        result["PhysicalAddressLine1"]!.Value<string>().ShouldBe("Newer Physical");
        result["MailingAddressLine1"]!.Value<string>().ShouldBe("Newer Mailing");
    }

    [Fact]
    public async Task Should_ReturnMostRecentlyModifiedFlaggedPrimary_WhenSeveralAreFlagged()
    {
        // Duplicate flags only arise from legacy data or a race, but the lookup must still match the
        // applicant details page, which favours the most recently modified address.
        GivenAddresses(
            Address(AddressType.PhysicalAddress, "Stale Primary", isPrimary: true, Older, lastModificationTime: Older),
            Address(AddressType.PhysicalAddress, "Recent Primary", isPrimary: true, Older, lastModificationTime: Newer));

        var result = await LookupAsync();

        result["PhysicalAddressLine1"]!.Value<string>().ShouldBe("Recent Primary");
    }

    [Fact]
    public async Task Should_ReturnEmptyAddressFields_WhenApplicantHasNoAddresses()
    {
        GivenAddresses();

        var result = await LookupAsync();

        result["PhysicalAddressLine1"]!.Value<string>().ShouldBeEmpty();
        result["MailingAddressLine1"]!.Value<string>().ShouldBeEmpty();
    }
}

using Shouldly;
using System;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.Applicants;

public class ApplicantAddressPrimaryResolverTests
{
    private static ApplicantAddress Address(
        AddressType addressType,
        bool isPrimary,
        DateTime creationTime,
        DateTime? lastModificationTime = null)
    {
        var address = new ApplicantAddress
        {
            AddressType = addressType,
            CreationTime = creationTime,
            LastModificationTime = lastModificationTime
        };
        EntityHelper.TrySetId(address, Guid.NewGuid);
        address.SetPrimaryFlag(isPrimary);
        return address;
    }

    [Fact]
    public void Should_SelectFlaggedPrimary_WhenAnotherAddressWasModifiedMoreRecently()
    {
        var primary = Address(
            AddressType.PhysicalAddress, isPrimary: true,
            creationTime: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            lastModificationTime: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var recentlyEdited = Address(
            AddressType.PhysicalAddress, isPrimary: false,
            creationTime: new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            lastModificationTime: new DateTime(2026, 9, 24, 0, 0, 0, DateTimeKind.Utc));

        var result = ApplicantAddressPrimaryResolver.Resolve(
            [recentlyEdited, primary], AddressType.PhysicalAddress);

        result.ShouldBe(primary);
    }

    [Fact]
    public void Should_FallBackToNewestByCreationTime_WhenNothingIsFlagged()
    {
        var older = Address(AddressType.MailingAddress, false, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = Address(AddressType.MailingAddress, false, new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = ApplicantAddressPrimaryResolver.Resolve(
            [older, newer], AddressType.MailingAddress);

        result.ShouldBe(newer);
    }

    [Fact]
    public void Should_IgnoreAddressesOfAnotherType()
    {
        var mailing = Address(AddressType.MailingAddress, isPrimary: true, new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = ApplicantAddressPrimaryResolver.Resolve(
            [mailing], AddressType.PhysicalAddress);

        result.ShouldBeNull();
    }
}

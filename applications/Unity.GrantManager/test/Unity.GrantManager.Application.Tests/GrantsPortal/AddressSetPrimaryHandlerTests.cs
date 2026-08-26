using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class AddressSetPrimaryHandlerTests
{
    private readonly IApplicantAddressRepository _addressRepository;
    private readonly AddressSetPrimaryHandler _handler;

    public AddressSetPrimaryHandlerTests()
    {
        _addressRepository = Substitute.For<IApplicantAddressRepository>();

        _addressRepository.UpdateAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ApplicantAddress>(0));

        _handler = new AddressSetPrimaryHandler(
            _addressRepository,
            new ApplicantAddressManager(_addressRepository),
            NullLogger<AddressSetPrimaryHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(
        Guid? addressId = null,
        Guid? profileId = null)
    {
        addressId ??= Guid.NewGuid();
        profileId ??= Guid.NewGuid();

        return new PluginDataPayload
        {
            Action = "ADDRESS_SET_PRIMARY_COMMAND",
            AddressId = addressId.Value.ToString(),
            ProfileId = profileId.Value.ToString(),
            Provider = Guid.NewGuid().ToString()
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldSetPrimaryOnTargetAddress()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var address = WithId(new ApplicantAddress { ApplicantId = applicantId }, addressId);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);
        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns(new List<ApplicantAddress>());

        var payload = CreatePayload(addressId: addressId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Address set as primary");
        address.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
        await _addressRepository.Received(1).UpdateAsync(address, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNoApplicantId_ShouldNotLookupSiblings()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var address = WithId(new ApplicantAddress { ApplicantId = null }, addressId);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);

        var payload = CreatePayload(addressId: addressId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Address set as primary");
        await _addressRepository.DidNotReceive().FindByApplicantIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSetProfileIdProperty()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var address = WithId(new ApplicantAddress { ApplicantId = null }, addressId);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);

        var payload = CreatePayload(addressId: addressId, profileId: profileId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        address.GetProperty<string>("profileId").ShouldBe(profileId.ToString());
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipSiblingsWithoutIsPrimaryProperty()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var siblingWithoutProp = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var address = WithId(new ApplicantAddress { ApplicantId = applicantId }, addressId);
        var sibling = WithId(new ApplicantAddress { ApplicantId = applicantId }, siblingWithoutProp);
        // sibling does NOT have isPrimary property

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);
        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns(new List<ApplicantAddress> { address, sibling });

        var payload = CreatePayload(addressId: addressId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert — sibling should not have been fetched for update
        await _addressRepository.DidNotReceive().GetAsync(siblingWithoutProp, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipSiblingsAlreadyNotPrimary()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var siblingId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var address = WithId(new ApplicantAddress { ApplicantId = applicantId }, addressId);
        var sibling = WithId(new ApplicantAddress { ApplicantId = applicantId }, siblingId);
        sibling.SetProperty(AddressExtraPropertyNames.IsPrimary, false);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);
        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns(new List<ApplicantAddress> { address, sibling });

        var payload = CreatePayload(addressId: addressId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert — sibling should not have been fetched for update since it's already not primary
        await _addressRepository.DidNotReceive().GetAsync(siblingId, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Primary scoped per address type

    [Fact]
    public async Task HandleAsync_ShouldDemoteOnlySiblingsOfTheSameAddressType()
    {
        var addressId = Guid.NewGuid();
        var sameTypeSiblingId = Guid.NewGuid();
        var otherTypeSiblingId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var address = WithId(
            new ApplicantAddress { ApplicantId = applicantId, AddressType = AddressType.BusinessAddress },
            addressId);

        var sameTypeSibling = WithId(
            new ApplicantAddress { ApplicantId = applicantId, AddressType = AddressType.BusinessAddress },
            sameTypeSiblingId);
        sameTypeSibling.SetProperty(AddressExtraPropertyNames.IsPrimary, true);

        var otherTypeSibling = WithId(
            new ApplicantAddress { ApplicantId = applicantId, AddressType = AddressType.MailingAddress },
            otherTypeSiblingId);
        otherTypeSibling.SetProperty(AddressExtraPropertyNames.IsPrimary, true);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);
        _addressRepository.GetAsync(sameTypeSiblingId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(sameTypeSibling);
        _addressRepository.GetAsync(otherTypeSiblingId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(otherTypeSibling);
        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, sameTypeSibling, otherTypeSibling]);

        var payload = CreatePayload(addressId: addressId);

        await _handler.HandleAsync(payload);

        sameTypeSibling.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
        otherTypeSibling.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
    }

    [Theory]
    [InlineData(AddressType.PhysicalAddress)]
    [InlineData(AddressType.MailingAddress)]
    [InlineData(AddressType.BusinessAddress)]
    public async Task HandleAsync_ShouldAllowOnePrimaryPerAddressType(AddressType addressType)
    {
        var addressId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var address = WithId(
            new ApplicantAddress { ApplicantId = applicantId, AddressType = addressType },
            addressId);

        var otherTypePrimaries = new List<ApplicantAddress> { address };

        foreach (var otherType in Enum.GetValues<AddressType>())
        {
            if (otherType == addressType)
            {
                continue;
            }

            var otherTypeSiblingId = Guid.NewGuid();
            var otherTypeSibling = WithId(
                new ApplicantAddress { ApplicantId = applicantId, AddressType = otherType },
                otherTypeSiblingId);
            otherTypeSibling.SetProperty(AddressExtraPropertyNames.IsPrimary, true);

            _addressRepository.GetAsync(otherTypeSiblingId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(otherTypeSibling);

            otherTypePrimaries.Add(otherTypeSibling);
        }

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);
        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns(otherTypePrimaries);

        var payload = CreatePayload(addressId: addressId);

        await _handler.HandleAsync(payload);

        otherTypePrimaries.ShouldAllBe(a => a.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary));
    }

    #endregion

    #region Validation

    [Fact]
    public async Task HandleAsync_WhenAddressIdMissing_ShouldThrow()
    {
        // Arrange
        var payload = CreatePayload();
        payload.AddressId = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(payload));
    }

    [Fact]
    public async Task HandleAsync_WhenProfileIdMissing_ShouldThrow()
    {
        // Arrange
        var payload = CreatePayload();
        payload.ProfileId = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(payload));
    }

    #endregion
}

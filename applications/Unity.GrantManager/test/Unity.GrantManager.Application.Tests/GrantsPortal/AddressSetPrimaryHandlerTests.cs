using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
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
        address.GetProperty<bool>("isPrimary").ShouldBeTrue();
        await _addressRepository.Received(1).UpdateAsync(address, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldClearPrimaryOnSiblingAddresses()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var siblingId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var address = WithId(new ApplicantAddress { ApplicantId = applicantId }, addressId);

        var sibling = WithId(new ApplicantAddress { ApplicantId = applicantId }, siblingId);
        sibling.SetProperty("isPrimary", true);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);
        _addressRepository.GetAsync(siblingId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(sibling);
        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns(new List<ApplicantAddress> { address, sibling });

        var payload = CreatePayload(addressId: addressId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert — sibling should have isPrimary cleared
        sibling.GetProperty<bool>("isPrimary").ShouldBeFalse();
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
        sibling.SetProperty("isPrimary", false);

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

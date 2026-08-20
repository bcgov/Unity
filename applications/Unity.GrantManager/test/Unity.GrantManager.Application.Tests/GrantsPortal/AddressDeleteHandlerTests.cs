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

public class AddressDeleteHandlerTests
{
    /// <summary>
    /// Re-election picks the most recently created remaining address of the deleted address's
    /// type, so only the relative order of these matters — they are named for the role they play.
    /// </summary>
    private static readonly DateTime Older = new(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Newer = new(2023, 6, 15, 14, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime Newest = new(2024, 3, 3, 8, 0, 0, DateTimeKind.Utc);

    private readonly IApplicantAddressRepository _addressRepository;
    private readonly AddressDeleteHandler _handler;

    public AddressDeleteHandlerTests()
    {
        _addressRepository = Substitute.For<IApplicantAddressRepository>();

        // Default: no existing address
        _addressRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((ApplicantAddress?)null);

        _handler = new AddressDeleteHandler(
            _addressRepository,
            new ApplicantAddressManager(_addressRepository),
            NullLogger<AddressDeleteHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(Guid? addressId = null)
    {
        addressId ??= Guid.NewGuid();

        return new PluginDataPayload
        {
            Action = "ADDRESS_DELETE_COMMAND",
            AddressId = addressId.Value.ToString(),
            ProfileId = Guid.NewGuid().ToString(),
            Provider = Guid.NewGuid().ToString()
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldDeleteAddress()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var address = WithId(new ApplicantAddress { City = "Victoria" }, addressId);

        _addressRepository.FindAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);

        var payload = CreatePayload(addressId: addressId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Address deleted successfully");
        await _addressRepository.Received(1).DeleteAsync(address, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenAddressDoesNotExist_ShouldNotThrow()
    {
        // Arrange — address not found (default mock returns null)
        var payload = CreatePayload();

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert — should still return success (idempotent delete)
        result.ShouldBe("Address deleted successfully");
        await _addressRepository.DidNotReceive().DeleteAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Primary re-election

    private ApplicantAddress StubAddress(Guid applicantId, AddressType addressType, bool isPrimary, DateTime? creationTime = null)
    {
        var id = Guid.NewGuid();
        var address = WithId(new ApplicantAddress { ApplicantId = applicantId, AddressType = addressType }, id);

        if (creationTime.HasValue)
        {
            address.CreationTime = creationTime.Value;
        }

        if (isPrimary)
        {
            address.SetProperty(AddressExtraPropertyNames.IsPrimary, true);
        }

        _addressRepository.FindAsync(id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(address);
        _addressRepository.GetAsync(id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(address);
        return address;
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedAddressWasPrimary_ShouldElectMostRecentOfSameTypeOnly()
    {
        var applicantId = Guid.NewGuid();

        var address = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: true);
        var olderMailing = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: false, creationTime: Older);
        var newerMailing = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: false, creationTime: Newer);

        // Deliberately the most recent address of ALL: if re-election ever stopped scoping by
        // type it would pick this one, so the assertion below only holds while scoping works.
        var physical = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: false, creationTime: Newest);

        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, olderMailing, newerMailing, physical]);

        var payload = CreatePayload(addressId: address.Id);

        await _handler.HandleAsync(payload);

        await _addressRepository.Received(1).DeleteAsync(address, Arg.Any<bool>(), Arg.Any<CancellationToken>());
        newerMailing.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
        olderMailing.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
        physical.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedAddressWasNotPrimary_ShouldNotElectAnotherPrimary()
    {
        var applicantId = Guid.NewGuid();

        var address = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: false);
        var otherMailing = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: false);

        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, otherMailing]);

        var payload = CreatePayload(addressId: address.Id);

        await _handler.HandleAsync(payload);

        otherMailing.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
        await _addressRepository.DidNotReceive().FindByApplicantIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task HandleAsync_WhenDeletedPrimaryHasNoApplicantId_ShouldNotLookupSiblings()
    {
        var addressId = Guid.NewGuid();
        var address = WithId(new ApplicantAddress { ApplicantId = null }, addressId);
        address.SetProperty(AddressExtraPropertyNames.IsPrimary, true);

        _addressRepository.FindAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);

        var payload = CreatePayload(addressId: addressId);

        await _handler.HandleAsync(payload);

        await _addressRepository.DidNotReceive().FindByApplicantIdAsync(Arg.Any<Guid>());
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

    #endregion
}

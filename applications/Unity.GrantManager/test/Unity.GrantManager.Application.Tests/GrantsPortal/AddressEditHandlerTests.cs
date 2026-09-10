using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
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

public class AddressEditHandlerTests
{
    /// <summary>
    /// Election picks the most recently created remaining address of a type, so only the
    /// relative order of these matters — they are named for the role they play.
    /// </summary>
    private static readonly DateTime Older = new(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Newer = new(2023, 6, 15, 14, 30, 0, DateTimeKind.Utc);

    private readonly IApplicantAddressRepository _addressRepository;
    private readonly AddressEditHandler _handler;

    public AddressEditHandlerTests()
    {
        _addressRepository = Substitute.For<IApplicantAddressRepository>();

        _addressRepository.UpdateAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ApplicantAddress>(0));

        _handler = new AddressEditHandler(
            _addressRepository,
            new ApplicantAddressManager(_addressRepository),
            NullLogger<AddressEditHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(
        Guid? addressId = null,
        JObject? data = null)
    {
        addressId ??= Guid.NewGuid();

        data ??= JObject.FromObject(new
        {
            street = "123 Main St",
            street2 = "Suite 100",
            unit = "4A",
            city = "Victoria",
            province = "BC",
            postalCode = "V8W 1A1",
            country = "Canada",
            addressType = "MAILING",
            isPrimary = true
        });

        return new PluginDataPayload
        {
            Action = "ADDRESS_EDIT_COMMAND",
            AddressId = addressId.Value.ToString(),
            ProfileId = Guid.NewGuid().ToString(),
            Provider = Guid.NewGuid().ToString(),
            Data = data
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldUpdateAddressFields()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var existingAddress = WithId(new ApplicantAddress
        {
            Street = "Old Street",
            City = "Old City"
        }, addressId);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(existingAddress);

        ApplicantAddress? updatedAddress = null;
        _addressRepository.UpdateAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                updatedAddress = ci.ArgAt<ApplicantAddress>(0);
                return updatedAddress;
            });

        var payload = CreatePayload(addressId: addressId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Address updated successfully");
        updatedAddress.ShouldNotBeNull();
        updatedAddress.Street.ShouldBe("123 Main St");
        updatedAddress.Street2.ShouldBe("Suite 100");
        updatedAddress.Unit.ShouldBe("4A");
        updatedAddress.City.ShouldBe("Victoria");
        updatedAddress.Province.ShouldBe("BC");
        updatedAddress.Postal.ShouldBe("V8W 1A1");
        updatedAddress.Country.ShouldBe("Canada");
        updatedAddress.AddressType.ShouldBe(AddressType.MailingAddress);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallUpdateOnRepository()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(WithId(new ApplicantAddress(), addressId));

        var payload = CreatePayload(addressId: addressId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        await _addressRepository.Received(1).UpdateAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Address type mapping

    [Theory]
    // The full mapping table (casing, every member, unrecognised and numeric values) is covered
    // by AddressTypeMapperTests. These two only prove the handler routes the payload value
    // through the mapper at all, and that an absent value still reaches the fallback.
    [InlineData("MAILING", AddressType.MailingAddress)]
    [InlineData(null, AddressType.PhysicalAddress)]
    public async Task HandleAsync_ShouldMapAddressTypeCorrectly(string? addressType, AddressType expected)
    {
        // Arrange
        var addressId = Guid.NewGuid();
        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(WithId(new ApplicantAddress(), addressId));

        ApplicantAddress? updatedAddress = null;
        _addressRepository.UpdateAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                updatedAddress = ci.ArgAt<ApplicantAddress>(0);
                return updatedAddress;
            });

        var data = JObject.FromObject(new
        {
            street = "123 Main St",
            city = "Victoria",
            province = "BC",
            postalCode = "V8W 1A1"
        });
        if (addressType != null)
        {
            data["addressType"] = addressType;
        }

        var payload = CreatePayload(addressId: addressId, data: data);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        updatedAddress.ShouldNotBeNull();
        updatedAddress.AddressType.ShouldBe(expected);
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
    public async Task HandleAsync_WhenDataMissing_ShouldThrow()
    {
        // Arrange
        var payload = CreatePayload();
        payload.Data = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(payload));
    }

    #endregion

    #region Primary tracking

    [Fact]
    public async Task HandleAsync_WhenIsPrimaryFalse_ShouldClearIsPrimary()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var address = WithId(new ApplicantAddress(), addressId);
        address.SetProperty(AddressExtraPropertyNames.IsPrimary, true);

        _addressRepository.GetAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(address);

        var data = JObject.FromObject(new
        {
            street = "123 Main St",
            city = "Victoria",
            province = "BC",
            postalCode = "V8W 1A1",
            addressType = "MAILING",
            isPrimary = false
        });

        var payload = CreatePayload(addressId: addressId, data: data);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        address.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
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
        await _handler.HandleAsync(payload);

        // Assert
        await _addressRepository.DidNotReceive().FindByApplicantIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipSiblingsWithoutIsPrimaryProperty()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var siblingWithoutProp = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var address = WithId(new ApplicantAddress { ApplicantId = applicantId }, addressId);

        // The sibling must share the type the payload edits the address INTO (MAILING), otherwise
        // it is excluded by type scoping and never reaches the isPrimary check under test here.
        // It deliberately has no isPrimary property at all.
        var sibling = WithId(
            new ApplicantAddress { ApplicantId = applicantId, AddressType = AddressType.MailingAddress },
            siblingWithoutProp);

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

    #endregion

    #region Primary scoped per address type

    private static JObject CreateAddressData(string addressType, bool isPrimary) => JObject.FromObject(new
    {
        street = "123 Main St",
        city = "Victoria",
        province = "BC",
        postalCode = "V8W 1A1",
        country = "Canada",
        addressType,
        isPrimary
    });

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

        _addressRepository.GetAsync(id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(address);
        return address;
    }

    [Fact]
    public async Task HandleAsync_WhenIsPrimaryTrue_ShouldPromoteAndDemoteOnlySameTypeSiblings()
    {
        var applicantId = Guid.NewGuid();

        var address = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: false);
        var mailingSibling = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: true);
        var physicalSibling = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: true);

        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, mailingSibling, physicalSibling]);

        var payload = CreatePayload(
            addressId: address.Id,
            data: CreateAddressData("MAILING", isPrimary: true));

        await _handler.HandleAsync(payload);

        // The edited address is promoted, its same-type sibling is demoted, and the primary
        // of the other type keeps its flag because exclusivity is scoped to the type group.
        address.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
        mailingSibling.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
        physicalSibling.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenPrimaryAddressChangesType_ShouldElectNewPrimaryInPreviousType()
    {
        var applicantId = Guid.NewGuid();

        var address = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: true);
        var olderPhysical = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: false, creationTime: Older);
        var newerPhysical = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: false, creationTime: Newer);

        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, olderPhysical, newerPhysical]);

        var payload = CreatePayload(
            addressId: address.Id,
            data: CreateAddressData("MAILING", isPrimary: true));

        await _handler.HandleAsync(payload);

        address.AddressType.ShouldBe(AddressType.MailingAddress);
        address.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
        newerPhysical.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
        olderPhysical.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenPrimaryAddressChangesType_ShouldDemoteExistingPrimaryInNewType()
    {
        var applicantId = Guid.NewGuid();

        var address = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: true);
        var mailingPrimary = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: true);

        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, mailingPrimary]);

        var payload = CreatePayload(
            addressId: address.Id,
            data: CreateAddressData("MAILING", isPrimary: true));

        await _handler.HandleAsync(payload);

        address.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
        mailingPrimary.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenNonPrimaryAddressChangesType_ShouldNotElectInPreviousType()
    {
        var applicantId = Guid.NewGuid();

        var address = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: false);
        var otherPhysical = StubAddress(applicantId, AddressType.PhysicalAddress, isPrimary: false);

        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, otherPhysical]);

        var payload = CreatePayload(
            addressId: address.Id,
            data: CreateAddressData("BUSINESS", isPrimary: false));

        await _handler.HandleAsync(payload);

        address.AddressType.ShouldBe(AddressType.BusinessAddress);
        otherPhysical.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WhenPrimaryAddressKeepsItsType_ShouldNotElectAnotherPrimaryInThatType()
    {
        var applicantId = Guid.NewGuid();

        var address = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: true);
        var otherMailing = StubAddress(applicantId, AddressType.MailingAddress, isPrimary: false);

        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns([address, otherMailing]);

        var payload = CreatePayload(
            addressId: address.Id,
            data: CreateAddressData("MAILING", isPrimary: true));

        await _handler.HandleAsync(payload);

        address.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
        otherMailing.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
    }

    #endregion
}

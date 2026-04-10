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

public class AddressCreateHandlerTests
{
    private readonly IApplicantAddressRepository _addressRepository;
    private readonly AddressCreateHandler _handler;

    public AddressCreateHandlerTests()
    {
        _addressRepository = Substitute.For<IApplicantAddressRepository>();

        // Default: no existing address
        _addressRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((ApplicantAddress?)null);
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ApplicantAddress>(0));
        _addressRepository.FindByApplicantIdAsync(Arg.Any<Guid>())
            .Returns(new List<ApplicantAddress>());

        _handler = new AddressCreateHandler(
            _addressRepository,
            NullLogger<AddressCreateHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(
        Guid? addressId = null,
        Guid? profileId = null,
        Guid? applicantId = null,
        JObject? data = null)
    {
        addressId ??= Guid.NewGuid();
        profileId ??= Guid.NewGuid();
        applicantId ??= Guid.NewGuid();

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
            isPrimary = true,
            applicantId = applicantId.Value
        });

        return new PluginDataPayload
        {
            Action = "ADDRESS_CREATE_COMMAND",
            AddressId = addressId.Value.ToString(),
            ProfileId = profileId.Value.ToString(),
            Provider = Guid.NewGuid().ToString(),
            Data = data
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldCreateAddress()
    {
        // Arrange
        ApplicantAddress? savedAddress = null;
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedAddress = ci.ArgAt<ApplicantAddress>(0);
                return savedAddress;
            });

        var payload = CreatePayload();

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Address created successfully");
        savedAddress.ShouldNotBeNull();
        savedAddress.Street.ShouldBe("123 Main St");
        savedAddress.Street2.ShouldBe("Suite 100");
        savedAddress.Unit.ShouldBe("4A");
        savedAddress.City.ShouldBe("Victoria");
        savedAddress.Province.ShouldBe("BC");
        savedAddress.Postal.ShouldBe("V8W 1A1");
        savedAddress.Country.ShouldBe("Canada");
        savedAddress.AddressType.ShouldBe(AddressType.MailingAddress);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallInsertOnRepository()
    {
        // Arrange
        var payload = CreatePayload();

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        await _addressRepository.Received(1).InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSetApplicantId()
    {
        // Arrange
        var applicantId = Guid.NewGuid();
        ApplicantAddress? savedAddress = null;
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedAddress = ci.ArgAt<ApplicantAddress>(0);
                return savedAddress;
            });

        var payload = CreatePayload(applicantId: applicantId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedAddress.ShouldNotBeNull();
        savedAddress.ApplicantId.ShouldBe(applicantId);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetEntityId()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        ApplicantAddress? savedAddress = null;
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedAddress = ci.ArgAt<ApplicantAddress>(0);
                return savedAddress;
            });

        var payload = CreatePayload(addressId: addressId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedAddress.ShouldNotBeNull();
        savedAddress.Id.ShouldBe(addressId);
    }

    [Fact]
    public async Task HandleAsync_ShouldSetProfileIdAndIsPrimaryProperties()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        ApplicantAddress? savedAddress = null;
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedAddress = ci.ArgAt<ApplicantAddress>(0);
                return savedAddress;
            });

        var payload = CreatePayload(profileId: profileId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedAddress.ShouldNotBeNull();
        savedAddress.GetProperty<string>(AddressExtraPropertyNames.ProfileId).ShouldBe(profileId.ToString());
        savedAddress.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
    }

    #endregion

    #region Idempotency

    [Fact]
    public async Task HandleAsync_WhenAddressAlreadyExists_ShouldReturnIdempotentMessage()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        _addressRepository.FindAsync(addressId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(WithId(new ApplicantAddress(), addressId));

        var payload = CreatePayload(addressId: addressId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Address already exists");
        await _addressRepository.DidNotReceive().InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Address type mapping

    [Theory]
    [InlineData("MAILING", AddressType.MailingAddress)]
    [InlineData("mailing", AddressType.MailingAddress)]
    [InlineData("PHYSICAL", AddressType.PhysicalAddress)]
    [InlineData("physical", AddressType.PhysicalAddress)]
    [InlineData("BUSINESS", AddressType.BusinessAddress)]
    [InlineData("business", AddressType.BusinessAddress)]
    [InlineData("UNKNOWN", AddressType.PhysicalAddress)]
    [InlineData(null, AddressType.PhysicalAddress)]
    public async Task HandleAsync_ShouldMapAddressTypeCorrectly(string? addressType, AddressType expected)
    {
        // Arrange
        ApplicantAddress? savedAddress = null;
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedAddress = ci.ArgAt<ApplicantAddress>(0);
                return savedAddress;
            });

        var data = JObject.FromObject(new
        {
            street = "123 Main St",
            city = "Victoria",
            province = "BC",
            postalCode = "V8W 1A1",
            applicantId = Guid.NewGuid()
        });
        if (addressType != null)
        {
            data["addressType"] = addressType;
        }

        var payload = CreatePayload(data: data);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedAddress.ShouldNotBeNull();
        savedAddress.AddressType.ShouldBe(expected);
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

    [Fact]
    public async Task HandleAsync_WhenDataMissing_ShouldThrow()
    {
        // Arrange
        var payload = CreatePayload();
        payload.Data = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(payload));
    }

    [Fact]
    public async Task HandleAsync_WhenApplicantIdEmpty_ShouldThrow()
    {
        // Arrange
        var data = JObject.FromObject(new
        {
            street = "123 Main St",
            city = "Victoria",
            province = "BC",
            postalCode = "V8W 1A1",
            applicantId = Guid.Empty
        });

        var payload = CreatePayload(data: data);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _handler.HandleAsync(payload));
    }

    #endregion

    #region Primary demotion

    [Fact]
    public async Task HandleAsync_WhenIsPrimaryTrue_ShouldDemoteSiblingAddresses()
    {
        // Arrange
        var addressId = Guid.NewGuid();
        var siblingId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var sibling = WithId(new ApplicantAddress { ApplicantId = applicantId }, siblingId);
        sibling.SetProperty(AddressExtraPropertyNames.IsPrimary, true);

        _addressRepository.GetAsync(siblingId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(sibling);
        _addressRepository.FindByApplicantIdAsync(applicantId)
            .Returns(new List<ApplicantAddress> { sibling });

        ApplicantAddress? savedAddress = null;
        _addressRepository.InsertAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedAddress = ci.ArgAt<ApplicantAddress>(0);
                return savedAddress;
            });

        var payload = CreatePayload(addressId: addressId, applicantId: applicantId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert — sibling should have isPrimary cleared
        sibling.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeFalse();
        savedAddress.ShouldNotBeNull();
        savedAddress.GetProperty<bool>(AddressExtraPropertyNames.IsPrimary).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenIsPrimaryFalse_ShouldNotDemoteSiblings()
    {
        // Arrange
        var applicantId = Guid.NewGuid();

        var data = JObject.FromObject(new
        {
            street = "123 Main St",
            city = "Victoria",
            province = "BC",
            postalCode = "V8W 1A1",
            country = "Canada",
            addressType = "MAILING",
            isPrimary = false,
            applicantId = applicantId
        });

        var payload = CreatePayload(data: data);

        // Act
        await _handler.HandleAsync(payload);

        // Assert — should not lookup siblings
        await _addressRepository.DidNotReceive().FindByApplicantIdAsync(Arg.Any<Guid>());
    }

    #endregion
}

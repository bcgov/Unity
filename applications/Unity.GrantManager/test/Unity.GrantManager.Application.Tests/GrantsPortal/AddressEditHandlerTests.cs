using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class AddressEditHandlerTests
{
    private readonly IApplicantAddressRepository _addressRepository;
    private readonly AddressEditHandler _handler;

    public AddressEditHandlerTests()
    {
        _addressRepository = Substitute.For<IApplicantAddressRepository>();

        _addressRepository.UpdateAsync(Arg.Any<ApplicantAddress>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ApplicantAddress>(0));

        _handler = new AddressEditHandler(
            _addressRepository,
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
}

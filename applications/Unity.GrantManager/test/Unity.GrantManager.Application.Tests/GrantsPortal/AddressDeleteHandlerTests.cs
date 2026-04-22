using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class AddressDeleteHandlerTests
{
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

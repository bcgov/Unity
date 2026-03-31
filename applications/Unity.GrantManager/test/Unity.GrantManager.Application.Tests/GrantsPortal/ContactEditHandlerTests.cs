using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class ContactEditHandlerTests
{
    private readonly IContactRepository _contactRepository;
    private readonly IContactLinkRepository _contactLinkRepository;
    private readonly ContactEditHandler _handler;

    public ContactEditHandlerTests()
    {
        _contactRepository = Substitute.For<IContactRepository>();
        _contactLinkRepository = Substitute.For<IContactLinkRepository>();

        _contactRepository.UpdateAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<Contact>(0));

        _contactLinkRepository.GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContactLink>());

        _handler = new ContactEditHandler(
            _contactRepository,
            _contactLinkRepository,
            NullLogger<ContactEditHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(
        Guid? contactId = null,
        JObject? data = null)
    {
        contactId ??= Guid.NewGuid();

        data ??= JObject.FromObject(new
        {
            name = "Updated Name",
            email = "updated@example.com",
            title = "Manager",
            homePhoneNumber = "444-4444",
            mobilePhoneNumber = "555-5555",
            workPhoneNumber = "666-6666",
            workPhoneExtension = "202",
            applicantId = Guid.NewGuid()
        });

        return new PluginDataPayload
        {
            Action = "CONTACT_EDIT_COMMAND",
            ContactId = contactId.Value.ToString(),
            ProfileId = Guid.NewGuid().ToString(),
            Provider = Guid.NewGuid().ToString(),
            Data = data
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldUpdateContactFields()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var existingContact = WithId(new Contact
        {
            Name = "Old Name",
            Email = "old@example.com"
        }, contactId);

        _contactRepository.GetAsync(contactId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(existingContact);

        Contact? updatedContact = null;
        _contactRepository.UpdateAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                updatedContact = ci.ArgAt<Contact>(0);
                return updatedContact;
            });

        var payload = CreatePayload(contactId: contactId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact updated successfully");
        updatedContact.ShouldNotBeNull();
        updatedContact.Name.ShouldBe("Updated Name");
        updatedContact.Email.ShouldBe("updated@example.com");
        updatedContact.Title.ShouldBe("Manager");
        updatedContact.HomePhoneNumber.ShouldBe("444-4444");
        updatedContact.MobilePhoneNumber.ShouldBe("555-5555");
        updatedContact.WorkPhoneNumber.ShouldBe("666-6666");
        updatedContact.WorkPhoneExtension.ShouldBe("202");
    }

    [Fact]
    public async Task HandleAsync_ShouldCallUpdateOnRepository()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        _contactRepository.GetAsync(contactId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(WithId(new Contact { Name = "Old" }, contactId));

        var payload = CreatePayload(contactId: contactId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        await _contactRepository.Received(1).UpdateAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation

    [Fact]
    public async Task HandleAsync_WhenContactIdMissing_ShouldThrow()
    {
        // Arrange
        var payload = CreatePayload();
        payload.ContactId = null;

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

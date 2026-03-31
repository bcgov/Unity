using Microsoft.Extensions.Logging.Abstractions;
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

public class ContactDeleteHandlerTests
{
    private readonly IContactRepository _contactRepository;
    private readonly IContactLinkRepository _contactLinkRepository;
    private readonly ContactDeleteHandler _handler;

    public ContactDeleteHandlerTests()
    {
        _contactRepository = Substitute.For<IContactRepository>();
        _contactLinkRepository = Substitute.For<IContactLinkRepository>();

        // Defaults: no contact, no links
        _contactRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((Contact?)null);
        _contactLinkRepository
            .GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContactLink>());

        _handler = new ContactDeleteHandler(
            _contactRepository,
            _contactLinkRepository,
            NullLogger<ContactDeleteHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(Guid? contactId = null)
    {
        contactId ??= Guid.NewGuid();

        return new PluginDataPayload
        {
            Action = "CONTACT_DELETE_COMMAND",
            ContactId = contactId.Value.ToString(),
            ProfileId = Guid.NewGuid().ToString(),
            Provider = Guid.NewGuid().ToString()
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldDeleteContactAndLinks()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = WithId(new Contact { Name = "To Delete" }, contactId);
        var links = new List<ContactLink>
        {
            WithId(new ContactLink { ContactId = contactId, RelatedEntityType = "Profile" }, Guid.NewGuid()),
            WithId(new ContactLink { ContactId = contactId, RelatedEntityType = "Profile" }, Guid.NewGuid())
        };

        _contactRepository.FindAsync(contactId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(contact);
        _contactLinkRepository
            .GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(links);

        var payload = CreatePayload(contactId: contactId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact deleted successfully");
        await _contactLinkRepository.Received(1).DeleteManyAsync(links, Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _contactRepository.Received(1).DeleteAsync(contact, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNoLinksExist_ShouldOnlyDeleteContact()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contact = WithId(new Contact { Name = "No Links" }, contactId);

        _contactRepository.FindAsync(contactId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(contact);
        // links default to empty list

        var payload = CreatePayload(contactId: contactId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact deleted successfully");
        await _contactLinkRepository.DidNotReceive().DeleteManyAsync(Arg.Any<List<ContactLink>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _contactRepository.Received(1).DeleteAsync(contact, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenContactDoesNotExist_ShouldNotThrow()
    {
        // Arrange — contact not found (default mock returns null)
        var payload = CreatePayload();

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert — should still return success (idempotent delete)
        result.ShouldBe("Contact deleted successfully");
        await _contactRepository.DidNotReceive().DeleteAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
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

    #endregion
}

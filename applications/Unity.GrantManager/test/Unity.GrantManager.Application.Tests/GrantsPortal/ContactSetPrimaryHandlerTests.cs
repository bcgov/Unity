using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class ContactSetPrimaryHandlerTests
{
    private readonly IContactLinkRepository _contactLinkRepository;
    private readonly ContactSetPrimaryHandler _handler;

    public ContactSetPrimaryHandlerTests()
    {
        _contactLinkRepository = Substitute.For<IContactLinkRepository>();

        _contactLinkRepository
            .GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContactLink>());

        _contactLinkRepository.UpdateAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ContactLink>(0));

        _handler = new ContactSetPrimaryHandler(
            _contactLinkRepository,
            NullLogger<ContactSetPrimaryHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(
        Guid? contactId = null,
        Guid? profileId = null,
        Guid? applicantId = null)
    {
        contactId ??= Guid.NewGuid();
        profileId ??= Guid.NewGuid();
        applicantId ??= Guid.NewGuid();

        return new PluginDataPayload
        {
            Action = "CONTACT_SET_PRIMARY_COMMAND",
            ContactId = contactId.Value.ToString(),
            ProfileId = profileId.Value.ToString(),
            Provider = Guid.NewGuid().ToString(),
            Data = JObject.FromObject(new { applicantId })
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldSetMatchingContactAsPrimary()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var otherContactId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var targetLink = WithId(new ContactLink
        {
            ContactId = contactId,
            RelatedEntityId = profileId,
            IsPrimary = false,
            IsActive = true
        }, Guid.NewGuid());

        var otherLink = WithId(new ContactLink
        {
            ContactId = otherContactId,
            RelatedEntityId = profileId,
            IsPrimary = true,
            IsActive = true
        }, Guid.NewGuid());

        _contactLinkRepository
            .GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContactLink> { targetLink, otherLink });

        var payload = CreatePayload(contactId: contactId, profileId: profileId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact set as primary");
        targetLink.IsPrimary.ShouldBeTrue();
        otherLink.IsPrimary.ShouldBeFalse();
        await _contactLinkRepository.Received(2).UpdateAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNoLinksExist_ShouldReturnSuccess()
    {
        // Arrange — default mock returns empty list
        var payload = CreatePayload();

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact set as primary");
        await _contactLinkRepository.DidNotReceive().UpdateAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldOnlySetTargetAsPrimary()
    {
        // Arrange — three links, only the target should be primary
        var contactId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var links = new List<ContactLink>
        {
            WithId(new ContactLink { ContactId = contactId, RelatedEntityId = profileId, IsPrimary = false, IsActive = true }, Guid.NewGuid()),
            WithId(new ContactLink { ContactId = Guid.NewGuid(), RelatedEntityId = profileId, IsPrimary = true, IsActive = true }, Guid.NewGuid()),
            WithId(new ContactLink { ContactId = Guid.NewGuid(), RelatedEntityId = profileId, IsPrimary = true, IsActive = true }, Guid.NewGuid())
        };

        _contactLinkRepository
            .GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(links);

        var payload = CreatePayload(contactId: contactId, profileId: profileId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        links.Single(l => l.ContactId == contactId).IsPrimary.ShouldBeTrue();
        links.Where(l => l.ContactId != contactId).ShouldAllBe(l => !l.IsPrimary);
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

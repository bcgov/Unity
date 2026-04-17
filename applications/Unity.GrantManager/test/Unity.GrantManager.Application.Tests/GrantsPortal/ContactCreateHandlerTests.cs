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

public class ContactCreateHandlerTests
{
    private readonly IContactRepository _contactRepository;
    private readonly IContactLinkRepository _contactLinkRepository;
    private readonly ContactCreateHandler _handler;

    public ContactCreateHandlerTests()
    {
        _contactRepository = Substitute.For<IContactRepository>();
        _contactLinkRepository = Substitute.For<IContactLinkRepository>();

        // Default: no existing contact
        _contactRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((Contact?)null);
        _contactRepository.InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<Contact>(0));
        _contactLinkRepository.InsertAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ContactLink>(0));
        _contactLinkRepository.GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContactLink>());
        _contactLinkRepository.UpdateAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ContactLink>(0));

        _handler = new ContactCreateHandler(
            _contactRepository,
            _contactLinkRepository,
            NullLogger<ContactCreateHandler>.Instance);
    }

    private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
    {
        EntityHelper.TrySetId(entity, () => id);
        return entity;
    }

    private static PluginDataPayload CreatePayload(
        Guid? contactId = null,
        string? profileId = null,
        string? subject = null,
        Guid? applicantId = null,
        JObject? data = null)
    {
        contactId ??= Guid.NewGuid();
        profileId ??= Guid.NewGuid().ToString();
        applicantId ??= Guid.NewGuid();

        data ??= JObject.FromObject(new
        {
            name = "Jane Doe",
            email = "jane@example.com",
            title = "Director",
            contactType = "Applicant",
            homePhoneNumber = "111-1111",
            mobilePhoneNumber = "222-2222",
            workPhoneNumber = "333-3333",
            workPhoneExtension = "101",
            role = "Primary Contact",
            isPrimary = true,
            applicantId = applicantId.Value
        });

        return new PluginDataPayload
        {
            Action = "CONTACT_CREATE_COMMAND",
            ContactId = contactId.Value.ToString(),
            ProfileId = profileId,
            Subject = subject,
            Provider = Guid.NewGuid().ToString(),
            Data = data
        };
    }

    #region Happy path

    [Fact]
    public async Task HandleAsync_ShouldCreateContactAndLink()
    {
        // Arrange
        var payload = CreatePayload();

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact created successfully");
        await _contactRepository.Received(1).InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _contactLinkRepository.Received(1).InsertAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSetContactFields()
    {
        // Arrange
        Contact? savedContact = null;
        _contactRepository.InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedContact = ci.ArgAt<Contact>(0);
                return savedContact;
            });

        var payload = CreatePayload();

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedContact.ShouldNotBeNull();
        savedContact.Name.ShouldBe("Jane Doe");
        savedContact.Email.ShouldBe("jane@example.com");
        savedContact.Title.ShouldBe("Director");
        savedContact.HomePhoneNumber.ShouldBe("111-1111");
        savedContact.MobilePhoneNumber.ShouldBe("222-2222");
        savedContact.WorkPhoneNumber.ShouldBe("333-3333");
        savedContact.WorkPhoneExtension.ShouldBe("101");
    }

    [Fact]
    public async Task HandleAsync_ShouldSetContactLinkFields()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        ContactLink? savedLink = null;

        _contactLinkRepository.InsertAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedLink = ci.ArgAt<ContactLink>(0);
                return savedLink;
            });

        var payload = CreatePayload(contactId: contactId, applicantId: applicantId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedLink.ShouldNotBeNull();
        savedLink.ContactId.ShouldBe(contactId);
        savedLink.RelatedEntityType.ShouldBe("Applicant");
        savedLink.RelatedEntityId.ShouldBe(applicantId);
        savedLink.Role.ShouldBe("Primary Contact");
        savedLink.IsPrimary.ShouldBeTrue();
        savedLink.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WhenIsPrimary_ShouldDemoteExistingPrimaryLinks()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var existingPrimary = WithId(new ContactLink
        {
            ContactId = Guid.NewGuid(),
            RelatedEntityId = applicantId,
            RelatedEntityType = "Applicant",
            IsPrimary = true,
            IsActive = true
        }, Guid.NewGuid());

        _contactLinkRepository
            .GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContactLink> { existingPrimary });

        var payload = CreatePayload(contactId: contactId, applicantId: applicantId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact created successfully");
        existingPrimary.IsPrimary.ShouldBeFalse();
        await _contactLinkRepository.Received(1).UpdateAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNotPrimary_ShouldNotDemoteExistingLinks()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        var data = JObject.FromObject(new
        {
            name = "Jane Doe",
            email = "jane@example.com",
            title = "Director",
            contactType = "Applicant",
            homePhoneNumber = "111-1111",
            mobilePhoneNumber = "222-2222",
            workPhoneNumber = "333-3333",
            workPhoneExtension = "101",
            role = "Secondary Contact",
            isPrimary = false,
            applicantId = applicantId
        });

        var payload = CreatePayload(contactId: contactId, applicantId: applicantId, data: data);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact created successfully");
        await _contactLinkRepository.DidNotReceive().GetListAsync(Arg.Any<Expression<Func<ContactLink, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _contactLinkRepository.DidNotReceive().UpdateAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Idempotency

    [Fact]
    public async Task HandleAsync_WhenContactAlreadyExists_ShouldReturnIdempotentSuccess()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        _contactRepository.FindAsync(contactId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(WithId(new Contact { Name = "Existing" }, contactId));

        var payload = CreatePayload(contactId: contactId);

        // Act
        var result = await _handler.HandleAsync(payload);

        // Assert
        result.ShouldBe("Contact already exists");
        await _contactRepository.DidNotReceive().InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _contactLinkRepository.DidNotReceive().InsertAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
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

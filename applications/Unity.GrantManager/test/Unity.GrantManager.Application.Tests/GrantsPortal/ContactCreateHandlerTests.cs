using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Handlers;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.GrantsPortal;

public class ContactCreateHandlerTests
{
    private readonly IContactRepository _contactRepository;
    private readonly IContactLinkRepository _contactLinkRepository;
    private readonly IApplicationFormSubmissionRepository _submissionRepository;
    private readonly IApplicantAgentRepository _agentRepository;
    private readonly ContactCreateHandler _handler;

    public ContactCreateHandlerTests()
    {
        _contactRepository = Substitute.For<IContactRepository>();
        _contactLinkRepository = Substitute.For<IContactLinkRepository>();
        _submissionRepository = Substitute.For<IApplicationFormSubmissionRepository>();
        _agentRepository = Substitute.For<IApplicantAgentRepository>();

        // Default: no existing contact, no submissions, no agents
        _contactRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((Contact?)null);
        _contactRepository.InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<Contact>(0));
        _contactLinkRepository.InsertAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<ContactLink>(0));
        _submissionRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicationFormSubmission, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApplicationFormSubmission>());
        _agentRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicantAgent, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApplicantAgent>());

        _handler = new ContactCreateHandler(
            _contactRepository,
            _contactLinkRepository,
            _submissionRepository,
            _agentRepository,
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
        JObject? data = null)
    {
        contactId ??= Guid.NewGuid();
        profileId ??= Guid.NewGuid().ToString();

        data ??= JObject.FromObject(new
        {
            name = "Jane Doe",
            email = "jane@example.com",
            title = "Director",
            contactType = "ApplicantProfile",
            homePhoneNumber = "111-1111",
            mobilePhoneNumber = "222-2222",
            workPhoneNumber = "333-3333",
            workPhoneExtension = "101",
            role = "Primary Contact",
            isPrimary = true
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
        var profileId = Guid.NewGuid().ToString();
        var contactId = Guid.NewGuid();
        ContactLink? savedLink = null;

        _contactLinkRepository.InsertAsync(Arg.Any<ContactLink>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedLink = ci.ArgAt<ContactLink>(0);
                return savedLink;
            });

        var payload = CreatePayload(contactId: contactId, profileId: profileId);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedLink.ShouldNotBeNull();
        savedLink.ContactId.ShouldBe(contactId);
        savedLink.RelatedEntityType.ShouldBe("ApplicantProfile");
        savedLink.RelatedEntityId.ShouldBe(Guid.Parse(profileId));
        savedLink.Role.ShouldBe("Primary Contact");
        savedLink.IsPrimary.ShouldBeTrue();
        savedLink.IsActive.ShouldBeTrue();
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

    #region Applicant agent ID lookup

    [Fact]
    public async Task HandleAsync_WhenSubmissionsExistWithAgents_ShouldSetApplicantAgentIds()
    {
        // Arrange — subject arrives as raw IDP value; OidcSub is stored normalized
        var rawSubject = "testuser@idir";
        var normalizedSub = "TESTUSER";
        var applicationId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var submission = new ApplicationFormSubmission
        {
            OidcSub = normalizedSub,
            ApplicationId = applicationId,
            ApplicantId = Guid.NewGuid(),
            ApplicationFormId = Guid.NewGuid(),
            ChefsSubmissionGuid = Guid.NewGuid().ToString()
        };

        var agent = WithId(new ApplicantAgent { ApplicationId = applicationId }, agentId);

        _submissionRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicationFormSubmission, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([submission]);
        _agentRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicantAgent, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns([agent]);

        Contact? savedContact = null;
        _contactRepository.InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedContact = ci.ArgAt<Contact>(0);
                return savedContact;
            });

        var payload = CreatePayload(subject: rawSubject);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedContact.ShouldNotBeNull();
        savedContact.ExtraProperties.ShouldContainKey("applicantAgentIds");
        var agentIds = (List<string>)savedContact.ExtraProperties["applicantAgentIds"]!;
        agentIds.ShouldContain(agentId.ToString());
    }

    [Fact]
    public async Task HandleAsync_WhenMultipleSubmissionsAndAgents_ShouldSetDistinctAgentIds()
    {
        // Arrange
        var rawSubject = "multiuser@idir";
        var normalizedSub = "MULTIUSER";
        var appId1 = Guid.NewGuid();
        var appId2 = Guid.NewGuid();
        var agentId1 = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();

        var submissions = new List<ApplicationFormSubmission>
        {
            new()
            {
                OidcSub = normalizedSub,
                ApplicationId = appId1,
                ApplicantId = Guid.NewGuid(),
                ApplicationFormId = Guid.NewGuid(),
                ChefsSubmissionGuid = Guid.NewGuid().ToString()
            },
            new()
            {
                OidcSub = normalizedSub,
                ApplicationId = appId2,
                ApplicantId = Guid.NewGuid(),
                ApplicationFormId = Guid.NewGuid(),
                ChefsSubmissionGuid = Guid.NewGuid().ToString()
            }
        };

        var agents = new List<ApplicantAgent>
        {
            WithId(new ApplicantAgent { ApplicationId = appId1 }, agentId1),
            WithId(new ApplicantAgent { ApplicationId = appId2 }, agentId2)
        };

        _submissionRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicationFormSubmission, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(submissions);
        _agentRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicantAgent, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(agents);

        Contact? savedContact = null;
        _contactRepository.InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedContact = ci.ArgAt<Contact>(0);
                return savedContact;
            });

        var payload = CreatePayload(subject: rawSubject);

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedContact.ShouldNotBeNull();
        savedContact.ExtraProperties.ShouldContainKey("applicantAgentIds");
        var agentIds = (List<string>)savedContact.ExtraProperties["applicantAgentIds"]!;
        agentIds.Count.ShouldBe(2);
        agentIds.ShouldContain(agentId1.ToString());
        agentIds.ShouldContain(agentId2.ToString());
    }

    [Fact]
    public async Task HandleAsync_WhenNoSubmissions_ShouldNotSetApplicantAgentIds()
    {
        // Arrange — default mock returns empty submissions
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
        savedContact.ExtraProperties.ShouldNotContainKey("applicantAgentIds");
    }

    [Fact]
    public async Task HandleAsync_WhenSubmissionsExistButNoAgents_ShouldNotSetApplicantAgentIds()
    {
        // Arrange
        var submission = new ApplicationFormSubmission
        {
            OidcSub = "SOMEUSER",
            ApplicationId = Guid.NewGuid(),
            ApplicantId = Guid.NewGuid(),
            ApplicationFormId = Guid.NewGuid(),
            ChefsSubmissionGuid = Guid.NewGuid().ToString()
        };

        _submissionRepository
            .GetListAsync(Arg.Any<Expression<Func<ApplicationFormSubmission, bool>>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ApplicationFormSubmission> { submission });
        // agents remain empty (default mock)

        Contact? savedContact = null;
        _contactRepository.InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedContact = ci.ArgAt<Contact>(0);
                return savedContact;
            });

        var payload = CreatePayload(subject: "someuser@idir");

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedContact.ShouldNotBeNull();
        savedContact.ExtraProperties.ShouldNotContainKey("applicantAgentIds");
    }

    [Fact]
    public async Task HandleAsync_WhenSubjectIsNull_ShouldNotSetApplicantAgentIds()
    {
        // Arrange — subject not provided
        Contact? savedContact = null;
        _contactRepository.InsertAsync(Arg.Any<Contact>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedContact = ci.ArgAt<Contact>(0);
                return savedContact;
            });

        var payload = CreatePayload();
        payload.Subject = null;

        // Act
        await _handler.HandleAsync(payload);

        // Assert
        savedContact.ShouldNotBeNull();
        savedContact.ExtraProperties.ShouldNotContainKey("applicantAgentIds");
    }

    #endregion

    #region NormalizeOidcSub

    [Theory]
    [InlineData("testuser@idir", "TESTUSER")]
    [InlineData("abc@bceidbusiness", "ABC")]
    [InlineData("ALREADY", "ALREADY")]
    [InlineData("mixedCase", "MIXEDCASE")]
    [InlineData("user@", "USER")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("@idir", null)]
    public void NormalizeOidcSub_ShouldStripIdpSuffixAndUppercase(string? input, string? expected)
    {
        ContactCreateHandler.NormalizeOidcSub(input).ShouldBe(expected);
    }

    #endregion
}

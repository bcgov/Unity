using System;
using Shouldly;
using Unity.Notifications.Emails;
using Unity.Notifications.Templates;
using Volo.Abp;
using Xunit;

namespace Unity.Notifications;

public class EmailOwnershipTests
{
    [Fact]
    public void Should_RejectMissingOrAmbiguousOwners()
    {
        Should.Throw<BusinessException>(() => EmailOwnership.EnsureSingleOwner(Guid.Empty, Guid.Empty));
        Should.Throw<BusinessException>(() => EmailOwnership.EnsureSingleOwner(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void Should_RejectMovingAnApplicantDraftToAnotherOwner()
    {
        var applicantId = Guid.NewGuid();
        var draft = new EmailLog { ApplicantId = applicantId, Status = EmailStatus.Draft };
        Should.NotThrow(() => EmailOwnership.EnsureDraftOwner(draft, Guid.Empty, applicantId));
        Should.Throw<BusinessException>(() => EmailOwnership.EnsureDraftOwner(draft, Guid.Empty, Guid.NewGuid()));
        Should.Throw<BusinessException>(() => EmailOwnership.EnsureDraftOwner(draft, Guid.NewGuid(), Guid.Empty));
    }

    [Theory]
    [InlineData(EmailStatus.Sent)]
    [InlineData(EmailStatus.Initialized)]
    [InlineData(EmailStatus.Scheduled)]
    [InlineData(EmailStatus.Cancelled)]
    public void Should_RejectEditingMessagesThatAreNotDrafts(string status)
    {
        var id = Guid.NewGuid();
        Should.Throw<BusinessException>(() => EmailOwnership.EnsureDraftOwner(
            new EmailLog { ApplicantId = id, Status = status }, Guid.Empty, id));
    }

    [Fact]
    public void Should_KeepApplicationDraftsCompatible()
    {
        var id = Guid.NewGuid();
        Should.NotThrow(() => EmailOwnership.EnsureDraftOwner(
            new EmailLog { ApplicationId = id, Status = EmailStatus.Draft }, id, Guid.Empty));
    }

    [Theory]
    [InlineData(TemplateTypes.Application, false)]
    [InlineData(TemplateTypes.Applicant, true)]
    [InlineData("applicant", true)]
    public void Should_RequireMatchingTemplateType(string templateType, bool applicantEmail)
    {
        var template = new EmailTemplate(Guid.NewGuid(), "Template", "", "Subject", "", "Body", "sender@example.test", templateType: templateType);
        var owner = applicantEmail ? Guid.NewGuid() : Guid.Empty;
        Should.NotThrow(() => EmailOwnership.EnsureTemplateType(template, owner));
        Should.Throw<BusinessException>(() => EmailOwnership.EnsureTemplateType(template, applicantEmail ? Guid.Empty : Guid.NewGuid()));
    }

    [Fact]
    public void Should_RetainTemplateIdentityInExistingLogMetadata()
    {
        var email = new EmailLog();
        EmailOwnership.GetTemplateId(email).ShouldBeNull();
        var templateId = Guid.NewGuid();
        EmailOwnership.SetTemplateId(email, templateId);
        EmailOwnership.GetTemplateId(email).ShouldBe(templateId);
    }
}

using Shouldly;
using System;
using Volo.Abp;
using Xunit;

namespace Unity.GrantManager.GrantApplications;

public class ComposedEmailValidatorTests
{
    [Fact]
    public void Should_AcceptCompleteEmail_WhenAllFieldsAreValid()
    {
        ComposedEmailValidator.ValidateFields(CreateValidEmail());
    }

    [Theory]
    [InlineData("")]
    [InlineData(";")]
    [InlineData("not-an-email")]
    public void Should_RejectToAddress_WhenMissingOrInvalid(string emailTo)
    {
        var email = CreateValidEmail();
        email.EmailTo = emailTo;

        var exception = Should.Throw<UserFriendlyException>(() => ComposedEmailValidator.ValidateFields(email));

        exception.Message.ShouldBe("The email is missing a valid To address.");
    }

    [Fact]
    public void Should_RejectSubject_WhenLengthExceedsLimit()
    {
        var email = CreateValidEmail();
        email.EmailSubject = new string('x', 1024);

        var exception = Should.Throw<UserFriendlyException>(() => ComposedEmailValidator.ValidateFields(email));

        exception.Message.ShouldBe("The email subject cannot exceed 1023 characters.");
    }

    [Theory]
    [InlineData("EmailCC", "invalid-cc", "The email contains an invalid CC address.")]
    [InlineData("EmailBCC", "invalid-bcc", "The email contains an invalid BCC address.")]
    public void Should_RejectOptionalRecipient_WhenAddressIsInvalid(
        string fieldName,
        string value,
        string expectedMessage)
    {
        var email = CreateValidEmail();
        typeof(ComposedEmailDto).GetProperty(fieldName)!.SetValue(email, value);

        var exception = Should.Throw<UserFriendlyException>(() => ComposedEmailValidator.ValidateFields(email));

        exception.Message.ShouldBe(expectedMessage);
    }

    [Theory]
    [InlineData("EmailFrom", "The email is missing a From address.")]
    [InlineData("EmailSubject", "The email is missing a subject.")]
    [InlineData("EmailBody", "The email is missing a body.")]
    public void Should_RejectRequiredField_WhenValueIsEmpty(string fieldName, string expectedMessage)
    {
        var email = CreateValidEmail();
        typeof(ComposedEmailDto).GetProperty(fieldName)!.SetValue(email, string.Empty);

        var exception = Should.Throw<UserFriendlyException>(() => ComposedEmailValidator.ValidateFields(email));

        exception.Message.ShouldBe(expectedMessage);
    }

    [Fact]
    public void Should_RejectTemplateAttachments_WhenTotalExceedsLimit()
    {
        var exception = Should.Throw<UserFriendlyException>(() =>
            ComposedEmailValidator.ValidateAttachmentSize(25_000_001, 25));

        exception.Message.ShouldContain("exceeds the maximum allowed 25 MB");
    }

    [Fact]
    public void Should_AcceptTemplateAttachments_WhenTotalEqualsLimit()
    {
        ComposedEmailValidator.ValidateAttachmentSize(25_000_000, 25);
    }

    private static ComposedEmailDto CreateValidEmail()
    {
        return new ComposedEmailDto
        {
            ApplicationId = Guid.NewGuid(),
            EmailTo = "applicant@example.com",
            EmailFrom = "sender@example.com",
            EmailSubject = "Application update",
            EmailBody = "<p>Your application has been updated.</p>"
        };
    }
}

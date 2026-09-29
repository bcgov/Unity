using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.Notifications.EmailGroups;
using Unity.Notifications.Emails;
using Unity.Notifications.Templates;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Settings;
using Xunit;

namespace Unity.GrantManager.Notifications;

public class ApplicantEmailTemplatePreviewTests
{
    [Fact]
    public async Task Should_RenderApplicantValuesAndRecipientsWithoutApplicationData()
    {
        var applicantId = Guid.NewGuid();
        var applicants = Substitute.For<IApplicantRepository>();
        applicants.GetAsync(applicantId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new Applicant { ApplicantName = "A & B", UnityApplicantId = "AP-1" });
        var applications = Substitute.For<IApplicationRepository>();
        var template = new EmailTemplate(Guid.NewGuid(), "Applicant template", "", "Hello {{applicant_name}}", "",
            "<p>{{applicant_name}} {{applicant_id}} {{submission_number}} {{unity_application_id}}</p>",
            "sender@example.test", "External", "ApplicationContact", TemplateTypes.Applicant);
        var templates = Substitute.For<ITemplatesRepository>();
        templates.GetByIdAsync(template.Id).Returns(template);
        var permissions = Substitute.For<IPermissionChecker>();
        permissions.IsGrantedAsync(Arg.Any<string>()).Returns(true);
        var features = Substitute.For<IFeatureChecker>();
        features.IsEnabledAsync("Unity.Notifications").Returns(true);
        var access = new EmailComposerAccessChecker(Substitute.For<IEmailLogsRepository>(), templates, applicants, applications,
            permissions, features, Substitute.For<ISettingProvider>());
        var contacts = Substitute.For<IApplicantContactQueryService>();
        contacts.GetByApplicantIdAsync(applicantId).Returns(new ApplicantContactInfoDto
        {
            Contacts = [new ContactInfoItemDto { IsPrimary = true, Email = "contact@example.test" }]
        });
        var service = new ApplicantEmailTemplateAppService(applicants, contacts, access,
            new ScheduledNotificationHelper(NullLoggerFactory.Instance), Substitute.For<IEmailGroupsAppService>(),
            Substitute.For<IEmailGroupUsersAppService>(), Substitute.For<IIdentityUserIntegrationService>());

        var preview = await service.GetPreviewAsync(applicantId, template.Id);

        preview.Subject.ShouldBe("Hello A & B");
        preview.Body.ShouldBe("<p>A &amp; B AP-1 {{submission_number}} {{unity_application_id}}</p>");
        preview.EmailTo.ShouldBe("contact@example.test");
        preview.TemplateType.ShouldBe(TemplateTypes.Applicant);
        await applications.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}

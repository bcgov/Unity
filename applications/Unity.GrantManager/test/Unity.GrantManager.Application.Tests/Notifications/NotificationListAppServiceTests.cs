using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.Notifications.Emails;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Notifications;

public class NotificationListAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly INotificationListAppService _notificationListAppService;
    private readonly IEmailLogsRepository _emailLogsRepository;
    private readonly IRepository<Application, Guid> _applicationRepository;

    public NotificationListAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _notificationListAppService = GetRequiredService<INotificationListAppService>();
        _emailLogsRepository = GetRequiredService<IEmailLogsRepository>();
        _applicationRepository = GetRequiredService<IRepository<Application, Guid>>();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_Should_Return_Notification_Joined_With_Application_ReferenceNo()
    {
        SetFeatureEnabled("Unity.Notifications", true);

        // Arrange: pick a seeded application and create an email log for it
        var application = (await _applicationRepository.GetListAsync())[0];
        var emailLog = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "Test Notification Subject",
            ToAddress = "applicant@example.com",
            FromAddress = "noreply@gov.bc.ca",
            Status = "Sent",
            Recipient = RecipientType.External,
            EmailType = EmailType.Manual
        }, autoSave: true);

        // Act
        var result = await _notificationListAppService.GetListAsync(
            new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });

        // Assert
        var row = result.Items.FirstOrDefault(i => i.Id == emailLog.Id);
        row.ShouldNotBeNull();
        row.Subject.ShouldBe("Test Notification Subject");
        row.SubmissionReferenceNo.ShouldBe(application.ReferenceNo);
        row.Recipient.ShouldBe(RecipientType.External);
        row.EmailType.ShouldBe(EmailType.Manual);
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_Should_Return_Row_Even_When_Application_Missing()
    {
        SetFeatureEnabled("Unity.Notifications", true);

        // Arrange: email log whose ApplicationId does not match any application (legacy/deleted)
        var orphanAppId = Guid.NewGuid();
        var emailLog = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = orphanAppId,
            ApplicantId = Guid.NewGuid(),
            Subject = "Orphan Notification",
            ToAddress = "x@example.com",
            FromAddress = "noreply@gov.bc.ca",
            Status = "Sent"
        }, autoSave: true);

        // Act
        var result = await _notificationListAppService.GetListAsync(
            new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });

        // Assert: row still present, with empty (not crashing) reference/applicant
        var row = result.Items.FirstOrDefault(i => i.Id == emailLog.Id);
        row.ShouldNotBeNull();
        row.SubmissionReferenceNo.ShouldBe(string.Empty);
        row.ApplicantName.ShouldBe(string.Empty);
    }
}

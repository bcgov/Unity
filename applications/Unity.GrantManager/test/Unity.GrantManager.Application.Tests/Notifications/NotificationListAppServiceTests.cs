using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Unity.GrantManager.Applications;
using Unity.Notifications.Emails;
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

        // Arrange: use the known seeded application (ReferenceNo "TEST12345", Applicant1)
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);
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
            new NotificationListInputDto { MaxResultCount = 1000 });

        // Assert
        var row = result.Items.FirstOrDefault(i => i.Id == emailLog.Id);
        row.ShouldNotBeNull();
        row.Subject.ShouldBe("Test Notification Subject");
        row.SubmissionReferenceNo.ShouldBe("TEST12345");
        row.Recipient.ShouldBe(RecipientType.External);
        row.EmailType.ShouldBe(EmailType.Manual);
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [InlineData(EmailType.Manual, "Manual")]
    [InlineData(EmailType.DateBased, "Scheduled")]
    [InlineData(EmailType.EventBased, "Event Based")]
    [InlineData(EmailType.Delayed, "Delayed")]
    public async Task GetListAsync_Should_Label_EmailType_Per_Taxonomy(EmailType emailType, string expectedLabel)
    {
        SetFeatureEnabled("Unity.Notifications", true);

        // Arrange: Manual, Scheduled (DateBased), Event Based (EventBased), Delayed
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);
        var emailLog = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "Email Type Label Test",
            ToAddress = "applicant@example.com",
            FromAddress = "noreply@gov.bc.ca",
            Status = "Sent",
            Recipient = RecipientType.Internal,
            EmailType = emailType
        }, autoSave: true);

        // Act
        var result = await _notificationListAppService.GetListAsync(
            new NotificationListInputDto { MaxResultCount = 1000 });

        // Assert
        var row = result.Items.FirstOrDefault(i => i.Id == emailLog.Id);
        row.ShouldNotBeNull();
        row.EmailType.ShouldBe(emailType);
        row.EmailTypeText.ShouldBe(expectedLabel);
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
            new NotificationListInputDto { MaxResultCount = 1000 });

        // Assert: row still present, with empty (not crashing) reference/applicant
        var row = result.Items.FirstOrDefault(i => i.Id == emailLog.Id);
        row.ShouldNotBeNull();
        row.SubmissionReferenceNo.ShouldBe(string.Empty);
        row.ApplicantName.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_Should_Only_Return_Rows_When_Sent_Within_The_Date_Range()
    {
        SetFeatureEnabled("Unity.Notifications", true);
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);

        var inRange = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "In Range",
            Status = "Sent",
            SentDateTime = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc)
        }, autoSave: true);

        var outOfRange = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "Out Of Range",
            Status = "Sent",
            SentDateTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        }, autoSave: true);

        var result = await _notificationListAppService.GetListAsync(new NotificationListInputDto
        {
            DateFrom = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified),
            DateTo = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Unspecified)
        });

        result.Items.ShouldContain(i => i.Id == inRange.Id);
        result.Items.ShouldNotContain(i => i.Id == outOfRange.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_Should_Include_Emails_Sent_Late_On_The_To_Date_Local_Time()
    {
        SetFeatureEnabled("Unity.Notifications", true);
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);

        // 2026-03-31 23:30 Vancouver (PDT, UTC-7) == 2026-04-01 06:30 UTC — still the 31st locally.
        var lateOnToDate = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "Late On To Date",
            Status = "Sent",
            SentDateTime = new DateTime(2026, 4, 1, 6, 30, 0, DateTimeKind.Utc)
        }, autoSave: true);

        // 2026-04-01 00:30 Vancouver == 2026-04-01 07:30 UTC — the next local day.
        var nextLocalDay = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "Next Local Day",
            Status = "Sent",
            SentDateTime = new DateTime(2026, 4, 1, 7, 30, 0, DateTimeKind.Utc)
        }, autoSave: true);

        var result = await _notificationListAppService.GetListAsync(new NotificationListInputDto
        {
            DateFrom = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Unspecified),
            DateTo = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Unspecified)
        });

        result.Items.ShouldContain(i => i.Id == lateOnToDate.Id);
        result.Items.ShouldNotContain(i => i.Id == nextLocalDay.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_Should_Return_All_Rows_When_No_Date_Range_Provided()
    {
        SetFeatureEnabled("Unity.Notifications", true);
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);

        var veryOld = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "Very Old",
            Status = "Sent",
            SentDateTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }, autoSave: true);

        var result = await _notificationListAppService.GetListAsync(new NotificationListInputDto());

        result.Items.ShouldContain(i => i.Id == veryOld.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetListAsync_Should_Include_Unsent_Rows_By_Creation_Date_Within_Range()
    {
        SetFeatureEnabled("Unity.Notifications", true);
        var application = await _applicationRepository.GetAsync(GrantManagerTestData.Application1_Id);

        // A draft / unsent notification has no SentDateTime. It must still appear in a bounded
        // window based on when it was created (CreationTime is stamped on insert), not vanish.
        var draft = await _emailLogsRepository.InsertAsync(new EmailLog
        {
            ApplicationId = application.Id,
            ApplicantId = application.ApplicantId,
            Subject = "Unsent Draft",
            Status = "Draft",
            SentDateTime = null
        }, autoSave: true);

        var result = await _notificationListAppService.GetListAsync(new NotificationListInputDto
        {
            DateFrom = DateTime.Today.AddDays(-2),
            DateTo = DateTime.Today.AddDays(2)
        });

        result.Items.ShouldContain(i => i.Id == draft.Id);
    }
}

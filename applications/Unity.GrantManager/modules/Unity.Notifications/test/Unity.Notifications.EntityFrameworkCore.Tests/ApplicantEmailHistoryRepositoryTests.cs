using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Unity.Notifications.Emails;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.Notifications;

public class ApplicantEmailHistoryRepositoryTests : NotificationsEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_IncludeManualAndAutomaticApplicantEmailsWithoutOtherOwnersOrTenants()
    {
        var repository = GetRequiredService<IEmailLogsRepository>();
        var tenant = GetRequiredService<ICurrentTenant>();
        var tenantId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        using (tenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await repository.InsertAsync(Log(applicantId, Guid.Empty, tenantId, "manual", EmailType.Manual), autoSave: true);
                await repository.InsertAsync(Log(applicantId, Guid.Empty, tenantId, "automatic", EmailType.EventBased), autoSave: true);
                await repository.InsertAsync(Log(applicantId, applicationId, tenantId, "application", EmailType.Manual), autoSave: true);
                await repository.InsertAsync(Log(Guid.NewGuid(), Guid.Empty, tenantId, "other applicant", EmailType.Manual), autoSave: true);
                await repository.InsertAsync(Log(Guid.Empty, Guid.Empty, tenantId, "system", EmailType.EventBased), autoSave: true);
            });
        }
        var otherTenantId = Guid.NewGuid();
        using (tenant.Change(otherTenantId))
        {
            await WithUnitOfWorkAsync(() => repository.InsertAsync(
                Log(applicantId, Guid.Empty, otherTenantId, "other tenant", EmailType.Manual), autoSave: true));
        }
        using (tenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var applicantHistory = await repository.GetByApplicantIdAsync(applicantId);
                applicantHistory.Select(e => e.Subject).OrderBy(s => s).ShouldBe(["automatic", "manual"]);
                var applicationHistory = await repository.GetByApplicationIdAsync(applicationId);
                applicationHistory.ShouldHaveSingleItem().Subject.ShouldBe("application");
            });
        }
    }

    [Fact]
    public async Task Should_RejectEmptyHistoryOwnerIds()
    {
        var repository = GetRequiredService<IEmailLogsRepository>();
        await Should.ThrowAsync<ArgumentException>(() => repository.GetByApplicantIdAsync(Guid.Empty));
        await Should.ThrowAsync<ArgumentException>(() => repository.GetByApplicationIdAsync(Guid.Empty));
    }

    private static EmailLog Log(Guid applicantId, Guid applicationId, Guid tenantId, string subject, EmailType type)
    {
        return new EmailLog
        {
            Id = Guid.NewGuid(), ApplicantId = applicantId, ApplicationId = applicationId, TenantId = tenantId,
            Subject = subject, EmailType = type, Status = EmailStatus.Sent
        };
    }
}

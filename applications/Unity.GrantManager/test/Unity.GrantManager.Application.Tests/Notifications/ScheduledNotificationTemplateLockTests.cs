using System;
using System.Threading.Tasks;
using Shouldly;
using Unity.Notifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Notifications;

public class ScheduledNotificationTemplateLockTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task ScheduleWrites_RequireActiveTemplate_AndBlockTemplateDeletion()
    {
        var templates = GetRequiredService<ITemplatesRepository>();
        var schedules = GetRequiredService<IRepository<ScheduledNotification, Guid>>();
        var uow = GetRequiredService<IUnitOfWorkManager>();
        var scheduleService = new AutomatedNotificationAppService(schedules, templates, uow);
        var templateService = new TemplateService(
            templates, GetRequiredService<ICurrentTenant>(),
            GetRequiredService<ITemplateVariablesRepository>(), uow, schedules);
        var template = new EmailTemplate(Guid.NewGuid(), "schedule template", "", "", "", "", "")
        {
            TenantId = GetRequiredService<ICurrentTenant>().Id
        };
        await templates.InsertAsync(template, autoSave: true);

        await Should.ThrowAsync<BusinessException>(() => scheduleService.CreateAsync(
            new CreateUpdateNotificationDto { EmailTemplateId = Guid.NewGuid() }));

        var schedule = await scheduleService.CreateAsync(
            new CreateUpdateNotificationDto { EmailTemplateId = template.Id });

        var inUse = await Should.ThrowAsync<BusinessException>(() => templateService.DeleteTemplate(template.Id));
        inUse.Code.ShouldBe(NotificationsErrorCodes.TemplateInUse);
        (await templates.GetAsync(template.Id)).ShouldNotBeNull();

        var deletedTemplate = new EmailTemplate(Guid.NewGuid(), "deleted template", "", "", "", "", "")
        {
            TenantId = GetRequiredService<ICurrentTenant>().Id
        };
        await templates.InsertAsync(deletedTemplate, autoSave: true);
        await templateService.DeleteTemplate(deletedTemplate.Id);

        await Should.ThrowAsync<BusinessException>(() => scheduleService.CreateAsync(
            new CreateUpdateNotificationDto { EmailTemplateId = deletedTemplate.Id }));
        await Should.ThrowAsync<BusinessException>(() => scheduleService.UpdateAsync(schedule.Id,
            new CreateUpdateNotificationDto { EmailTemplateId = deletedTemplate.Id }));
        (await schedules.GetAsync(schedule.Id)).EmailTemplateId.ShouldBe(template.Id);
    }

    [Theory]
    [InlineData(EmailStatus.Sent)]
    [InlineData(EmailStatus.Cancelled)]
    public async Task CancelledSchedules_AllowTemplateDeletion_AndRequireAnAvailableTemplateToReactivate(string emailStatus)
    {
        var templates = GetRequiredService<ITemplatesRepository>();
        var schedules = GetRequiredService<IRepository<ScheduledNotification, Guid>>();
        var emails = GetRequiredService<IEmailLogsRepository>();
        var tenant = GetRequiredService<ICurrentTenant>();
        var uow = GetRequiredService<IUnitOfWorkManager>();
        var scheduleService = new AutomatedNotificationAppService(schedules, templates, uow);
        var templateService = new TemplateService(
            templates, tenant, GetRequiredService<ITemplateVariablesRepository>(), uow, schedules);
        var template = new EmailTemplate(Guid.NewGuid(), "cancelled schedule template", "", "", "", "", "")
        {
            TenantId = tenant.Id
        };
        await templates.InsertAsync(template, autoSave: true);

        var firstSchedule = await scheduleService.CreateAsync(
            new CreateUpdateNotificationDto { EmailTemplateId = template.Id });
        var secondSchedule = await scheduleService.CreateAsync(
            new CreateUpdateNotificationDto { EmailTemplateId = template.Id });
        var email = new EmailLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            ApplicationId = GrantManagerTestData.Application1_Id,
            ScheduledNotificationId = firstSchedule.Id,
            TemplateName = template.Name,
            Subject = "Stored subject",
            Body = "<p>Stored body</p>",
            Status = emailStatus
        };
        EmailOwnership.SetTemplateId(email, template.Id);
        await emails.InsertAsync(email, autoSave: true);

        // Delivery status does not retire an active plan.
        var activePlanError = await Should.ThrowAsync<BusinessException>(
            () => templateService.DeleteTemplate(template.Id));
        activePlanError.Code.ShouldBe(NotificationsErrorCodes.TemplateInUse);

        await scheduleService.CancelAsync(firstSchedule.Id);
        var remainingActivePlanError = await Should.ThrowAsync<BusinessException>(
            () => templateService.DeleteTemplate(template.Id));
        remainingActivePlanError.Code.ShouldBe(NotificationsErrorCodes.TemplateInUse);

        await scheduleService.CancelAsync(secondSchedule.Id);
        await templateService.DeleteTemplate(template.Id);

        (await templateService.GetTemplateById(template.Id)).ShouldBeNull();
        (await templateService.GetTemplateById(Guid.NewGuid())).ShouldBeNull();
        foreach (var scheduleId in new[] { firstSchedule.Id, secondSchedule.Id })
        {
            var cancelledPlan = await schedules.GetAsync(scheduleId);
            cancelledPlan.IsActive.ShouldBeFalse();
            cancelledPlan.EmailTemplateId.ShouldBe(template.Id);
        }

        var retainedEmail = await emails.GetAsync(email.Id);
        retainedEmail.Subject.ShouldBe(email.Subject);
        retainedEmail.Body.ShouldBe(email.Body);
        retainedEmail.Status.ShouldBe(emailStatus);
        retainedEmail.TemplateName.ShouldBe(template.Name);
        retainedEmail.ScheduledNotificationId.ShouldBe(firstSchedule.Id);
        EmailOwnership.GetTemplateId(retainedEmail).ShouldBe(template.Id);

        var reactivationError = await Should.ThrowAsync<BusinessException>(() => scheduleService.UpdateAsync(
            firstSchedule.Id, new CreateUpdateNotificationDto { EmailTemplateId = template.Id, IsActive = true }));
        reactivationError.Code.ShouldBe(NotificationsErrorCodes.TemplateNotAvailable);
        (await schedules.GetAsync(firstSchedule.Id)).IsActive.ShouldBeFalse();

        var replacement = new EmailTemplate(Guid.NewGuid(), "replacement template", "", "", "", "", "")
        {
            TenantId = tenant.Id
        };
        await templates.InsertAsync(replacement, autoSave: true);

        await scheduleService.UpdateAsync(firstSchedule.Id,
            new CreateUpdateNotificationDto { EmailTemplateId = replacement.Id, IsActive = true });

        var reactivatedPlan = await schedules.GetAsync(firstSchedule.Id);
        reactivatedPlan.IsActive.ShouldBeTrue();
        reactivatedPlan.EmailTemplateId.ShouldBe(replacement.Id);
        EmailOwnership.GetTemplateId(await emails.GetAsync(email.Id)).ShouldBe(template.Id);
    }
}

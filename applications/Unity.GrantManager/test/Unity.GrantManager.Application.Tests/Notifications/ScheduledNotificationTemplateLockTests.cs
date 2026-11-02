using System;
using System.Threading.Tasks;
using Shouldly;
using Unity.Notifications;
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
}

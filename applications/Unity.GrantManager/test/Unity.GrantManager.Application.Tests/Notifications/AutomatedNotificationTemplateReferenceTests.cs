using Microsoft.AspNetCore.Authorization;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Notifications.Permissions;
using Unity.Notifications.Templates;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Notifications;

public class AutomatedNotificationTemplateReferenceTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private static string[] PoliciesOf(string methodName) =>
        typeof(AutomatedNotificationAppService).GetMethod(methodName)!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Policy!)
            .ToArray();

    [Fact]
    public void Should_Require_Email_Send_For_Template_Reference_Lookup()
    {
        PoliciesOf(nameof(AutomatedNotificationAppService.GetTemplateReferencesAsync))
            .ShouldBe([NotificationsPermissions.Email.Template.Default]);
    }

    [Fact]
    public void Should_Keep_Schedule_List_Protected_By_Notifications_Tab()
    {
        PoliciesOf(nameof(AutomatedNotificationAppService.GetListAsync))
            .ShouldBe([NotificationsPermissions.Email.NotificationsTab]);
    }

    [Fact]
    public async Task Should_Return_Only_References_For_Requested_Template()
    {
        var schedules = GetRequiredService<IRepository<ScheduledNotification, Guid>>();
        var service = GetRequiredService<IAutomatedNotificationAppService>();
        var templates = GetRequiredService<ITemplatesRepository>();
        var tenantId = GetRequiredService<ICurrentTenant>().Id;
        var templateId = Guid.NewGuid();
        var otherTemplateId = Guid.NewGuid();

        await WithUnitOfWorkAsync(async () =>
        {
            foreach (var id in new[] { templateId, otherTemplateId })
            {
                await templates.InsertAsync(new EmailTemplate(id, $"template {id}", "", "", "", "", "") { TenantId = tenantId }, autoSave: true);
            }

            await schedules.InsertAsync(new ScheduledNotification { EmailTemplateId = templateId, TriggerType = "Event", TriggerDetail = "Approved", IsActive = true }, autoSave: true);
            await schedules.InsertAsync(new ScheduledNotification { EmailTemplateId = templateId, TriggerType = "Date", IsActive = false }, autoSave: true);
            await schedules.InsertAsync(new ScheduledNotification { EmailTemplateId = otherTemplateId, TriggerType = "Event", IsActive = true }, autoSave: true);
        });

        var references = await WithUnitOfWorkAsync(() => service.GetTemplateReferencesAsync(templateId));

        references.Count.ShouldBe(2);
        references.ShouldContain(r => r.TriggerType == "Event" && r.TriggerDetail == "Approved" && r.IsActive);
        references.ShouldContain(r => r.TriggerType == "Date" && !r.IsActive);
    }
}

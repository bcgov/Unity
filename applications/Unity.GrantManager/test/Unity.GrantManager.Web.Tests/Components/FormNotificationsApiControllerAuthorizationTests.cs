using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.GrantManager.Web.Controllers;
using Unity.Notifications.Permissions;
using Xunit;

namespace Unity.GrantManager.Components;

public class FormNotificationsApiControllerAuthorizationTests
{
    private static string[] PoliciesOf(string actionName) =>
        typeof(FormNotificationsApiController).GetMethod(actionName)!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Policy!)
            .ToArray();

    [Theory]
    [InlineData(nameof(FormNotificationsApiController.CanDeleteTemplate))]
    [InlineData(nameof(FormNotificationsApiController.GetTemplateNotificationPlans))]
    public void Should_Authorize_Template_Editors_For_Template_Preflights(string actionName)
    {
        PoliciesOf(actionName).ShouldBe([NotificationsPermissions.Email.Default]);
    }

    [Fact]
    public void Should_Authorize_Schedule_Actions_With_Notification_Email_Permissions()
    {
        PoliciesOf(nameof(FormNotificationsApiController.GetForForm))
            .ShouldBe([NotificationsPermissions.Email.NotificationsTab]);
        PoliciesOf(nameof(FormNotificationsApiController.CreateForForm))
            .ShouldBe([NotificationsPermissions.Email.ScheduleCreate]);
        PoliciesOf(nameof(FormNotificationsApiController.UpdateForForm))
            .ShouldBe([NotificationsPermissions.Email.ScheduleCreate]);
        PoliciesOf(nameof(FormNotificationsApiController.Delete))
            .ShouldBe([NotificationsPermissions.Email.ScheduleCancel]);
        PoliciesOf(nameof(FormNotificationsApiController.CancelNotification))
            .ShouldBe([NotificationsPermissions.Email.ScheduleCancel]);
    }

    [Fact]
    public async Task Should_Not_Use_Schedule_List_For_Template_Preflights()
    {
        var service = NSubstitute.Substitute.For<Notifications.IAutomatedNotificationAppService>();
        var templateId = Guid.NewGuid();
        service.GetTemplateReferencesAsync(templateId).Returns(
        [
            new Notifications.TemplateNotificationReferenceDto { TriggerType = "Event", TriggerDetail = "Approved", IsActive = true }
        ]);
        var controller = new FormNotificationsApiController(
            null!, null!, null!, service, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await controller.CanDeleteTemplate(templateId);

        result.Result.ShouldBeOfType<Microsoft.AspNetCore.Mvc.OkObjectResult>();
        await service.DidNotReceiveWithAnyArgs().GetListAsync(default!);
        await service.Received(1).GetTemplateReferencesAsync(templateId);
    }
}

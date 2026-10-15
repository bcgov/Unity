using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using Unity.GrantManager.Permissions;
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
        PoliciesOf(actionName).ShouldBe([NotificationsPermissions.Email.Send]);
    }

    [Fact]
    public void Should_Keep_Schedule_List_Protected_By_Scheduler_Permission()
    {
        PoliciesOf(nameof(FormNotificationsApiController.GetForForm))
            .ShouldBe([GrantManagerPermissions.NotificationScheduler.Default]);
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

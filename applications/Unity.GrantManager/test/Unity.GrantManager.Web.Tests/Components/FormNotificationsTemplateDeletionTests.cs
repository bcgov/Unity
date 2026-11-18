using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Notifications;
using Unity.GrantManager.Web.Controllers;
using Unity.Notifications.Templates;
using Volo.Abp.Application.Dtos;
using Xunit;

namespace Unity.GrantManager.Components;

public class FormNotificationsTemplateDeletionTests
{
    private readonly IAutomatedNotificationAppService _notifications = Substitute.For<IAutomatedNotificationAppService>();
    private readonly ITemplateService _templates = Substitute.For<ITemplateService>();
    private readonly IApplicationStatusService _statuses = Substitute.For<IApplicationStatusService>();
    private readonly FormNotificationsApiController _controller;

    public FormNotificationsTemplateDeletionTests()
    {
        _statuses.GetListAsync().Returns(new List<ApplicationStatusDto>());
        _controller = new FormNotificationsApiController(
            _statuses, null!, _templates, _notifications, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Should_AllowDeletion_WhenThereAreNoActiveReferences(int cancelledPlanCount)
    {
        var templateId = Guid.NewGuid();
        _notifications.GetTemplateReferencesAsync(templateId).Returns(
            Enumerable.Range(0, cancelledPlanCount)
                .Select(_ => new TemplateNotificationReferenceDto { TriggerType = "Date", IsActive = false })
                .ToList());

        var result = (await _controller.CanDeleteTemplate(templateId)).Result.ShouldBeOfType<OkObjectResult>();
        var response = JsonSerializer.SerializeToElement(result.Value);

        response.GetProperty("canDelete").GetBoolean().ShouldBeTrue();
        response.GetProperty("errorMessage").ValueKind.ShouldBe(JsonValueKind.Null);
        response.GetProperty("notificationPlanNames").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Should_BlockDeletion_AndListOnlyActiveReferences()
    {
        var templateId = Guid.NewGuid();
        _notifications.GetTemplateReferencesAsync(templateId).Returns(
        [
            new TemplateNotificationReferenceDto { TriggerType = "Date", TriggerDetail = "DueDate", IsActive = false },
            new TemplateNotificationReferenceDto { TriggerType = "Event", TriggerDetail = "Approved", IsActive = true }
        ]);

        var result = (await _controller.CanDeleteTemplate(templateId)).Result.ShouldBeOfType<OkObjectResult>();
        var response = JsonSerializer.SerializeToElement(result.Value);

        response.GetProperty("canDelete").GetBoolean().ShouldBeFalse();
        response.GetProperty("errorMessage").GetString().ShouldBe(
            "This template cannot be deleted because it is assigned to one or more active Scheduled Notifications. Please cancel these Scheduled Notifications or assign them a different template before deleting.");
        response.GetProperty("notificationPlanNames").EnumerateArray()
            .Select(value => value.GetString()).ToArray().ShouldBe(new[] { "Event notification - Approved" });
    }

    [Fact]
    public async Task Should_ListCancelledPlansWithDeletedTemplates_AlongsideAvailableTemplates()
    {
        var formId = Guid.NewGuid();
        var deletedTemplateId = Guid.NewGuid();
        var availableTemplate = new EmailTemplate(Guid.NewGuid(), "Available template", "", "", "", "", "");
        var cancelledPlan = new NotificationDto
        {
            Id = Guid.NewGuid(), FormId = formId, EmailTemplateId = deletedTemplateId,
            IsActive = false, TriggerType = "Date", DateField = "DueDate"
        };
        var activePlan = new NotificationDto
        {
            Id = Guid.NewGuid(), FormId = formId, EmailTemplateId = availableTemplate.Id,
            IsActive = true, TriggerType = "Event", Module = "Application"
        };
        _notifications.GetListAsync(Arg.Any<GetNotificationsInput>()).Returns(
            new PagedResultDto<NotificationDto>(2, new[] { cancelledPlan, activePlan }));
        _templates.GetTemplateById(deletedTemplateId).Returns((EmailTemplate?)null);
        _templates.GetTemplateById(availableTemplate.Id).Returns(availableTemplate);

        var result = (await _controller.GetForForm(formId.ToString())).Result.ShouldBeOfType<OkObjectResult>();
        var plans = result.Value.ShouldBeOfType<List<ScheduledNotificationDto>>();

        plans.Count.ShouldBe(2);
        var retainedPlan = plans.Single(plan => plan.Id == cancelledPlan.Id);
        retainedPlan.TemplateId.ShouldBe(deletedTemplateId);
        retainedPlan.TemplateName.ShouldBe("Template deleted");
        retainedPlan.IsActive.ShouldBeFalse();
        retainedPlan.DateType.ShouldBe("DueDate");
        plans.Single(plan => plan.Id == activePlan.Id).TemplateName.ShouldBe(availableTemplate.Name);
    }

    [Fact]
    public async Task Should_RejectReactivationWithAnUnavailableTemplate()
    {
        var templateId = Guid.NewGuid();
        _templates.GetTemplateById(templateId).Returns((EmailTemplate?)null);

        var result = (await _controller.UpdateForForm(Guid.NewGuid().ToString(), Guid.NewGuid(),
            new CreateScheduledNotificationInput { TemplateId = templateId, TriggerType = "Date" })).Result;

        result.ShouldBeOfType<BadRequestObjectResult>();
        await _notifications.DidNotReceive().UpdateAsync(Arg.Any<Guid>(), Arg.Any<CreateUpdateNotificationDto>());
    }

    [Fact]
    public async Task Should_AllowReplacingADeletedTemplateWhenReactivating()
    {
        var formId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var deletedTemplateId = Guid.NewGuid();
        var replacement = new EmailTemplate(Guid.NewGuid(), "Replacement", "", "", "", "", "");
        _templates.GetTemplateById(replacement.Id).Returns(replacement);
        _templates.GetTemplateById(deletedTemplateId).Returns((EmailTemplate?)null);
        _notifications.GetAsync(planId).Returns(new NotificationDto
        {
            Id = planId, FormId = formId, EmailTemplateId = deletedTemplateId, IsActive = false
        });
        _notifications.UpdateAsync(planId, Arg.Any<CreateUpdateNotificationDto>()).Returns(new NotificationDto
        {
            Id = planId, FormId = formId, EmailTemplateId = replacement.Id, IsActive = true,
            TriggerType = "Date", DateField = "DueDate"
        });

        var result = (await _controller.UpdateForForm(formId.ToString(), planId,
            new CreateScheduledNotificationInput
            {
                TemplateId = replacement.Id, TriggerType = "Date", DateType = "DueDate"
            })).Result.ShouldBeOfType<OkObjectResult>();

        var plan = result.Value.ShouldBeOfType<ScheduledNotificationDto>();
        plan.TemplateId.ShouldBe(replacement.Id);
        plan.TemplateName.ShouldBe(replacement.Name);
        plan.IsActive.ShouldBeTrue();
        await _notifications.Received(1).UpdateAsync(planId,
            Arg.Is<CreateUpdateNotificationDto>(input => input.EmailTemplateId == replacement.Id && input.IsActive));
    }
}

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Volo.Abp.Domain.Entities;
using Volo.Abp.TenantManagement;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Intakes;

public class SubmissionAppServiceReconcileTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    private const string TenantName = "CGG";

    private ITenantRepository _tenantRepository = null!;
    private IIntakeSubmissionAppService _intakeSubmissionAppService = null!;

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);

        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantRepository.GetListAsync().ReturnsForAnyArgs([CreateTenant(Guid.NewGuid(), TenantName)]);
        _intakeSubmissionAppService = Substitute.For<IIntakeSubmissionAppService>();

        services.AddSingleton(_tenantRepository);
        services.AddSingleton(_intakeSubmissionAppService);
    }

    [Fact]
    public async Task Should_Create_Intake_With_Chefs_Event_Payload_When_Submission_Missing()
    {
        var item = CreateItem("FAA0B483");
        _intakeSubmissionAppService.CreateIntakeSubmissionAsync(Arg.Any<EventSubscriptionDto>())
            .Returns(new EventSubscriptionConfirmationDto { ConfirmationId = Guid.NewGuid() });

        var results = await GetRequiredService<ISubmissionAppService>()
            .ReconcileSubmissionsAsync(CreateInput(item));

        results.Count.ShouldBe(1);
        results[0].Success.ShouldBeTrue();
        results[0].ConfirmationId.ShouldBe("FAA0B483");
        await _intakeSubmissionAppService.Received(1).CreateIntakeSubmissionAsync(Arg.Is<EventSubscriptionDto>(e =>
            e.FormId == item.FormId &&
            e.FormVersion == item.FormVersionId &&
            e.SubmissionId == item.SubmissionId));
    }

    [Fact]
    public async Task Should_Report_Failure_When_Intake_Rejects_Submission()
    {
        _intakeSubmissionAppService.CreateIntakeSubmissionAsync(Arg.Any<EventSubscriptionDto>())
            .Returns(new EventSubscriptionConfirmationDto { ExceptionMessage = "An Error Occured Validating the Chefs Submission" });

        var results = await GetRequiredService<ISubmissionAppService>()
            .ReconcileSubmissionsAsync(CreateInput(CreateItem("FC55EEE0")));

        results[0].Success.ShouldBeFalse();
        results[0].Message.ShouldBe("An Error Occured Validating the Chefs Submission");
    }

    [Fact]
    public async Task Should_Continue_With_Remaining_Submissions_When_One_Throws()
    {
        var failing = CreateItem("FAIL0001");
        var passing = CreateItem("PASS0001");
        _intakeSubmissionAppService.CreateIntakeSubmissionAsync(Arg.Is<EventSubscriptionDto>(e => e.SubmissionId == failing.SubmissionId))
            .ThrowsAsync(new InvalidOperationException("CHEFS unavailable"));
        _intakeSubmissionAppService.CreateIntakeSubmissionAsync(Arg.Is<EventSubscriptionDto>(e => e.SubmissionId == passing.SubmissionId))
            .Returns(new EventSubscriptionConfirmationDto { ConfirmationId = Guid.NewGuid() });

        var results = await GetRequiredService<ISubmissionAppService>()
            .ReconcileSubmissionsAsync(CreateInput(failing, passing));

        results.Count.ShouldBe(2);
        results[0].Success.ShouldBeFalse();
        results[0].Message.ShouldNotBeNullOrWhiteSpace();
        results[1].Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Fail_All_Submissions_When_Tenant_Not_Found()
    {
        var input = CreateInput(CreateItem("FAA0B483"), CreateItem("FC55EEE0"));
        input.TenantName = "Unknown";

        var results = await GetRequiredService<ISubmissionAppService>().ReconcileSubmissionsAsync(input);

        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => !r.Success);
        await _intakeSubmissionAppService.DidNotReceiveWithAnyArgs().CreateIntakeSubmissionAsync(default!);
    }

    private static ReconcileSubmissionsInput CreateInput(params ReconcileSubmissionItemDto[] items)
    {
        return new ReconcileSubmissionsInput
        {
            TenantName = TenantName,
            Submissions = new List<ReconcileSubmissionItemDto>(items)
        };
    }

    private static ReconcileSubmissionItemDto CreateItem(string confirmationId)
    {
        return new ReconcileSubmissionItemDto
        {
            SubmissionId = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            FormVersionId = Guid.NewGuid(),
            ConfirmationId = confirmationId
        };
    }

    private static Tenant CreateTenant(Guid id, string name)
    {
        var tenant = (Tenant)Activator.CreateInstance(typeof(Tenant), nonPublic: true)!;
        EntityHelper.TrySetId(tenant, () => id);
        typeof(Tenant).GetProperty(nameof(Tenant.Name))!.SetValue(tenant, name);
        return tenant;
    }
}

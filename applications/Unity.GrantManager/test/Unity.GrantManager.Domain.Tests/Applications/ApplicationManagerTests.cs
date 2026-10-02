using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.GrantManager.Applications;

public class ApplicationManagerTests
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly ApplicationManager _sut;

    public ApplicationManagerTests()
    {
        _applicationRepository = Substitute.For<IApplicationRepository>();

        _sut = new ApplicationManager(
            _applicationRepository,
            Substitute.For<IApplicationStatusRepository>(),
            Substitute.For<IApplicationAssignmentRepository>(),
            Substitute.For<IUnitOfWorkManager>(),
            Substitute.For<IPersonRepository>(),
            Substitute.For<IPermissionChecker>());
    }

    private static Application CreateApplication(
        GrantApplicationState state,
        DateTime? finalDecisionDate = null,
        string? declineRational = null,
        bool isDirectApproval = false) => new()
        {
            ApplicationStatus = new ApplicationStatus { StatusCode = state },
            ApplicationForm = new ApplicationForm { IsDirectApproval = isDirectApproval },
            FinalDecisionDate = finalDecisionDate,
            DeclineRational = declineRational
        };

    [Fact]
    public async Task TriggerAction_Should_Throw_When_Deny_Without_DeclineRational()
    {
        var application = CreateApplication(GrantApplicationState.ASSESSMENT_COMPLETED, finalDecisionDate: DateTime.Now);
        _applicationRepository.GetAsync(Arg.Any<Guid>()).Returns(application);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            _sut.TriggerAction(Guid.NewGuid(), GrantApplicationAction.Deny));

        exception.Message.ShouldContain("Decline Rationale");
    }

    [Fact]
    public async Task TriggerAction_Should_Throw_When_Approve_Without_FinalDecisionDate()
    {
        var application = CreateApplication(GrantApplicationState.ASSESSMENT_COMPLETED);
        _applicationRepository.GetAsync(Arg.Any<Guid>()).Returns(application);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            _sut.TriggerAction(Guid.NewGuid(), GrantApplicationAction.Approve));

        exception.Message.ShouldContain("Decision Date");
    }

    [Fact]
    public async Task TriggerAction_Should_Throw_When_Deny_Without_FinalDecisionDate_Even_With_DeclineRational_Supplied()
    {
        var application = CreateApplication(GrantApplicationState.ASSESSMENT_COMPLETED);
        _applicationRepository.GetAsync(Arg.Any<Guid>()).Returns(application);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            _sut.TriggerAction(Guid.NewGuid(), GrantApplicationAction.Deny, declineRational: "NOT_ALIGNED"));

        exception.Message.ShouldContain("Decision Date");
    }

    [Fact]
    public async Task TriggerAction_Should_Set_Supplied_Fields_Before_State_Transition()
    {
        // The CLOSED state does not permit Approve/Deny for a non-direct-approval form, so the
        // state machine itself throws once past our required-field checks.
        var application = CreateApplication(GrantApplicationState.CLOSED);
        _applicationRepository.GetAsync(Arg.Any<Guid>()).Returns(application);

        var decisionDate = DateTime.Now.AddDays(-1);

        var exception = await Record.ExceptionAsync(() =>
            _sut.TriggerAction(Guid.NewGuid(), GrantApplicationAction.Deny, decisionDate, "NOT_ALIGNED"));

        exception.ShouldNotBeNull();
        exception.ShouldNotBeOfType<UserFriendlyException>();
        application.FinalDecisionDate.ShouldBe(decisionDate);
        application.DeclineRational.ShouldBe("NOT_ALIGNED");
    }

    [Fact]
    public async Task TriggerAction_Should_Not_Overwrite_Existing_DeclineRational_When_Not_Supplied()
    {
        var application = CreateApplication(GrantApplicationState.CLOSED, finalDecisionDate: DateTime.Now, declineRational: "OTHER");
        _applicationRepository.GetAsync(Arg.Any<Guid>()).Returns(application);

        var exception = await Record.ExceptionAsync(() =>
            _sut.TriggerAction(Guid.NewGuid(), GrantApplicationAction.Deny));

        exception.ShouldNotBeNull();
        exception.ShouldNotBeOfType<UserFriendlyException>();
        application.DeclineRational.ShouldBe("OTHER");
    }
}

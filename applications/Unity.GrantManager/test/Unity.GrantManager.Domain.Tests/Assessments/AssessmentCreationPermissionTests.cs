using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Guids;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Xunit;

namespace Unity.GrantManager.Assessments;

public class AssessmentCreationPermissionTests
{
    private readonly IAssessmentRepository _assessments = Substitute.For<IAssessmentRepository>();
    private readonly IApplicationManager _applications = Substitute.For<IApplicationManager>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IUserData _user = Substitute.For<IUserData>();
    private readonly List<Assessment> _existing = [];
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly AssessmentManager _manager;

    public AssessmentCreationPermissionTests()
    {
        _user.Id.Returns(Guid.NewGuid());
        _assessments.GetListByApplicationId(_applicationId).Returns(_existing);
        _assessments.InsertAsync(Arg.Any<Assessment>(), true, Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Assessment>());
        var forms = Substitute.For<IApplicationFormRepository>();
        forms.GetAsync(Arg.Any<Guid>()).Returns(new ApplicationForm());
        // Exercise the real workflow's transition rules rather than replacing them with booleans.
        var workflow = new ApplicationManager(Substitute.For<IApplicationRepository>(),
            Substitute.For<IApplicationStatusRepository>(), Substitute.For<IApplicationAssignmentRepository>(),
            Substitute.For<IUnitOfWorkManager>(), Substitute.For<Unity.GrantManager.Identity.IPersonRepository>(),
            Substitute.For<IPermissionChecker>());
        _applications.IsActionAllowed(Arg.Any<Application>(), GrantApplicationAction.Internal_StartAssessment)
            .Returns(call => workflow.IsActionAllowed(call.Arg<Application>(), GrantApplicationAction.Internal_StartAssessment));
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetService<IGuidGenerator>(Arg.Any<IGuidGenerator>()).Returns(SimpleGuidGenerator.Instance);
        lazy.LazyGetRequiredService<IGuidGenerator>().Returns(SimpleGuidGenerator.Instance);
        _manager = new AssessmentManager(_applications, _assessments, forms,
            Substitute.For<ILocalEventBus>(), Substitute.For<IFeatureChecker>(), _permissions)
        {
            LazyServiceProvider = lazy
        };
    }

    [Theory]
    [InlineData(GrantApplicationState.SUBMITTED, false)]
    [InlineData(GrantApplicationState.SUBMITTED, true)]
    [InlineData(GrantApplicationState.ASSIGNED, false)]
    [InlineData(GrantApplicationState.UNDER_INITIAL_REVIEW, false)]
    [InlineData(GrantApplicationState.INITITAL_REVIEW_COMPLETED, false)]
    public async Task Should_Reject_Starting_Assessment_Before_Inserting_A_Record(GrantApplicationState state, bool hasAiAssessment)
    {
        var application = ApplicationIn(state);
        if (hasAiAssessment)
        {
            _existing.Add(new Assessment(Guid.NewGuid(), _applicationId, AIScoringConstants.AiPersonId) { IsAiAssessment = true });
        }

        (await _manager.RequiresStartAssessmentPermissionAsync(application)).ShouldBeTrue();
        var error = await Should.ThrowAsync<BusinessException>(() => _manager.CreateAsync(application, _user));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.StartAssessmentPermissionRequired);
        application.ApplicationStatus.StatusCode.ShouldBe(state);
        await _assessments.DidNotReceiveWithAnyArgs().InsertAsync(default!, default, default);
        await _applications.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);
    }

    [Theory]
    [InlineData(GrantApplicationState.SUBMITTED)]
    [InlineData(GrantApplicationState.ASSIGNED)]
    [InlineData(GrantApplicationState.UNDER_INITIAL_REVIEW)]
    [InlineData(GrantApplicationState.INITITAL_REVIEW_COMPLETED)]
    public async Task Should_Create_And_Start_Assessment_When_Permission_Is_Granted(GrantApplicationState state)
    {
        _permissions.IsGrantedAsync(GrantApplicationPermissions.Assessments.Start).Returns(true);

        var assessment = await _manager.CreateAsync(ApplicationIn(state), _user);

        assessment.AssessorId.ShouldBe(_user.Id);
        await _assessments.Received(1).InsertAsync(assessment, true, Arg.Any<CancellationToken>());
        await _applications.Received(1).TriggerAction(_applicationId, GrantApplicationAction.Internal_StartAssessment);
    }

    [Theory]
    [InlineData(GrantApplicationState.UNDER_ASSESSMENT)]
    [InlineData(GrantApplicationState.ASSESSMENT_COMPLETED)]
    [InlineData(GrantApplicationState.DEFER)]
    [InlineData(GrantApplicationState.ON_HOLD)]
    public async Task Should_Allow_Creation_Without_Start_Permission_When_No_Status_Transition_Occurs(GrantApplicationState state)
    {
        var application = ApplicationIn(state);

        (await _manager.RequiresStartAssessmentPermissionAsync(application)).ShouldBeFalse();
        await _manager.CreateAsync(application, _user);

        application.ApplicationStatus.StatusCode.ShouldBe(state);
        await _permissions.DidNotReceive().IsGrantedAsync(GrantApplicationPermissions.Assessments.Start);
        await _applications.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);
    }

    [Fact]
    public async Task Should_Not_Require_Start_Permission_For_An_Additional_Human_Assessment()
    {
        _existing.Add(new Assessment(Guid.NewGuid(), _applicationId, Guid.NewGuid()));
        var application = ApplicationIn(GrantApplicationState.SUBMITTED);

        (await _manager.RequiresStartAssessmentPermissionAsync(application)).ShouldBeFalse();
        await _manager.CreateAsync(application, _user);

        application.ApplicationStatus.StatusCode.ShouldBe(GrantApplicationState.SUBMITTED);
        await _permissions.DidNotReceive().IsGrantedAsync(GrantApplicationPermissions.Assessments.Start);
        await _applications.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);
    }

    [Theory]
    [InlineData(GrantApplicationState.GRANT_APPROVED)]
    [InlineData(GrantApplicationState.GRANT_NOT_APPROVED)]
    [InlineData(GrantApplicationState.CLOSED)]
    [InlineData(GrantApplicationState.WITHDRAWN)]
    [InlineData(GrantApplicationState.RESOLVED)]
    public async Task Should_Still_Reject_Final_State_Applications(GrantApplicationState state)
    {
        _permissions.IsGrantedAsync(GrantApplicationPermissions.Assessments.Start).Returns(true);

        var error = await Should.ThrowAsync<BusinessException>(() => _manager.CreateAsync(ApplicationIn(state), _user));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.CantCreateAssessmentForFinalStateApplication);
        await _assessments.DidNotReceiveWithAnyArgs().InsertAsync(default!, default, default);
    }

    [Fact]
    public async Task Should_Still_Reject_Duplicate_User_Assessments()
    {
        _assessments.ExistsAsync(_applicationId, _user.Id).Returns(true);

        var error = await Should.ThrowAsync<BusinessException>(() =>
            _manager.CreateAsync(ApplicationIn(GrantApplicationState.UNDER_ASSESSMENT), _user));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.AssessmentUserAssignmentAlreadyExists);
        await _assessments.DidNotReceiveWithAnyArgs().InsertAsync(default!, default, default);
    }

    private Application ApplicationIn(GrantApplicationState state) => new ApplicationSeed(_applicationId)
    {
        ApplicationForm = new ApplicationForm(),
        ApplicationStatus = new ApplicationStatus { StatusCode = state }
    };
}

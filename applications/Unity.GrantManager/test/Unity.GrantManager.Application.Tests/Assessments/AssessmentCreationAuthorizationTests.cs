using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Permissions;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Localization;
using Unity.GrantManager.Permissions;
using Unity.Modules.Shared;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.Guids;
using Volo.Abp.Identity.Integration;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Xunit;

namespace Unity.GrantManager.Assessments;

public class AssessmentCreationAuthorizationTests
{
    private const string StartPermissionMessage = "You need the \"Start Assessment\" permission to create an assessment and move this application to Under Assessment.";
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _aiAssessmentId = Guid.NewGuid();
    private readonly IAssessmentRepository _repository = Substitute.For<IAssessmentRepository>();
    private readonly IApplicationManager _applications = Substitute.For<IApplicationManager>();
    private readonly IAssessmentScoresheetService _scoresheets = Substitute.For<IAssessmentScoresheetService>();
    private readonly List<Assessment> _assessments = [];
    private readonly Application _application;
    private readonly AssessmentAppService _service;
    private bool _canStart;

    public AssessmentCreationAuthorizationTests()
    {
        _application = new ApplicationSeed(_applicationId)
        {
            ApplicationForm = new ApplicationForm(),
            ApplicationStatus = new ApplicationStatus { StatusCode = GrantApplicationState.SUBMITTED }
        };
        var aiAssessment = new Assessment(_aiAssessmentId, _applicationId, AIScoringConstants.AiPersonId) { IsAiAssessment = true };
        _assessments.Add(aiAssessment);
        _repository.GetAsync(_aiAssessmentId).Returns(aiAssessment);
        _repository.GetListByApplicationId(_applicationId).Returns(_ => _assessments.ToList());
        _repository.ExistsAsync(_applicationId, _userId).Returns(_ => _assessments.Any(assessment => assessment.AssessorId == _userId));
        _repository.InsertAsync(Arg.Any<Assessment>(), true, Arg.Any<CancellationToken>()).Returns(call =>
        {
            var assessment = call.Arg<Assessment>();
            _assessments.Add(assessment);
            return assessment;
        });
        var applicationRepository = Substitute.For<IApplicationRepository>();
        applicationRepository.GetAsync(_applicationId, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(_application);
        var forms = Substitute.For<IApplicationFormRepository>();
        forms.GetAsync(Arg.Any<Guid>()).Returns(new ApplicationForm());
        var workflow = new ApplicationManager(applicationRepository, Substitute.For<IApplicationStatusRepository>(),
            Substitute.For<IApplicationAssignmentRepository>(), Substitute.For<IUnitOfWorkManager>(),
            Substitute.For<Unity.GrantManager.Identity.IPersonRepository>(), Substitute.For<IPermissionChecker>());
        _applications.IsActionAllowed(Arg.Any<Application>(), GrantApplicationAction.Internal_StartAssessment)
            .Returns(call => workflow.IsActionAllowed(call.Arg<Application>(), GrantApplicationAction.Internal_StartAssessment));
        var permissions = Substitute.For<IPermissionChecker>();
        permissions.IsGrantedAsync(GrantApplicationPermissions.Assessments.Start).Returns(_ => _canStart);
        var features = Substitute.For<IFeatureChecker>();
        features.IsEnabledAsync("Unity.AI.Scoring").Returns(true);
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetService<IGuidGenerator>(Arg.Any<IGuidGenerator>()).Returns(SimpleGuidGenerator.Instance);
        lazy.LazyGetRequiredService<IGuidGenerator>().Returns(SimpleGuidGenerator.Instance);
        var manager = new AssessmentManager(_applications, _repository, forms,
            Substitute.For<ILocalEventBus>(), features, permissions) { LazyServiceProvider = lazy };
        var user = new UserData(_userId, "assessor", "assessor@example.test");
        var lookup = Substitute.For<IIdentityUserIntegrationService>();
        lookup.FindByIdAsync(_userId).Returns(user);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(_userId);
        lazy.LazyGetRequiredService<ICurrentUser>().Returns(currentUser);
        var authorization = Substitute.For<IAbpAuthorizationService>();
        authorization.CurrentPrincipal.Returns(new ClaimsPrincipal());
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), GrantApplicationPermissions.Assessments.Start)
            .Returns(_ => _canStart ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        lazy.LazyGetRequiredService<IAuthorizationService>().Returns(authorization);
        var mapper = Substitute.For<IObjectMapper>();
        mapper.Map<Assessment, AssessmentDto>(Arg.Any<Assessment>()).Returns(new AssessmentDto());
        lazy.LazyGetService<IObjectMapper>(Arg.Any<Func<IServiceProvider, object>>()).Returns(mapper);
        var localizer = Substitute.For<IStringLocalizer<GrantManagerResource>>();
        localizer[Arg.Any<string>()].Returns(call =>
        {
            var key = call.Arg<string>();
            return new LocalizedString(key, key == GrantManagerDomainErrorCodes.StartAssessmentPermissionRequired ? StartPermissionMessage : key);
        });
        var localizerFactory = Substitute.For<IStringLocalizerFactory>();
        localizerFactory.Create(typeof(GrantManagerResource)).Returns(localizer);
        lazy.LazyGetRequiredService<IStringLocalizerFactory>().Returns(localizerFactory);
        _service = new AssessmentAppService(_repository, manager, applicationRepository, lookup, features, _scoresheets)
        {
            LazyServiceProvider = lazy
        };
    }

    [Fact]
    public async Task Should_Explain_Why_Submitted_Application_Creation_Is_Disabled()
    {
        var availability = await _service.GetCreationAvailabilityAsync(_applicationId);

        availability.CanCreate.ShouldBeFalse();
        availability.Reason.ShouldBe(StartPermissionMessage);
        _assessments.Count.ShouldBe(1);
        await _applications.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Reject_Create_And_Clone_Before_Inserting_An_Assessment(bool clone)
    {
        var error = await Should.ThrowAsync<BusinessException>(() => CreateAsync(clone));

        error.Code.ShouldBe(GrantManagerDomainErrorCodes.StartAssessmentPermissionRequired);
        _assessments.Count.ShouldBe(1);
        _application.ApplicationStatus.StatusCode.ShouldBe(GrantApplicationState.SUBMITTED);
        await _applications.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);
        await _scoresheets.DidNotReceiveWithAnyArgs().CopyAiAnswersIfEnabledAsync(default, default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Allow_Create_And_Clone_With_Start_Permission(bool clone)
    {
        _canStart = true;
        (await _service.GetCreationAvailabilityAsync(_applicationId)).CanCreate.ShouldBeTrue();

        await CreateAsync(clone);

        _assessments.Count(assessment => !assessment.IsAiAssessment).ShouldBe(1);
        await _applications.Received(1).TriggerAction(_applicationId, GrantApplicationAction.Internal_StartAssessment);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Should_Allow_Create_And_Clone_Once_Application_Is_Under_Assessment(bool clone)
    {
        _application.ApplicationStatus.StatusCode = GrantApplicationState.UNDER_ASSESSMENT;
        var availability = await _service.GetCreationAvailabilityAsync(_applicationId);
        availability.CanCreate.ShouldBeTrue();
        availability.Reason.ShouldBeNull();

        await CreateAsync(clone);

        _assessments.Count(assessment => !assessment.IsAiAssessment).ShouldBe(1);
        await _applications.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);
    }

    [Theory]
    [InlineData(GrantApplicationState.GRANT_APPROVED)]
    [InlineData(GrantApplicationState.GRANT_NOT_APPROVED)]
    [InlineData(GrantApplicationState.CLOSED)]
    [InlineData(GrantApplicationState.WITHDRAWN)]
    [InlineData(GrantApplicationState.RESOLVED)]
    public async Task Should_Disable_Creation_For_Final_State_Applications(GrantApplicationState state)
    {
        _canStart = true;
        _application.ApplicationStatus.StatusCode = state;

        var availability = await _service.GetCreationAvailabilityAsync(_applicationId);

        availability.CanCreate.ShouldBeFalse();
        availability.Reason.ShouldBe("Assessment:FinalState");
    }

    [Fact]
    public async Task Should_Disable_Creation_For_A_User_Who_Already_Has_An_Assessment()
    {
        _canStart = true;
        _assessments.Add(new Assessment(Guid.NewGuid(), _applicationId, _userId));

        var availability = await _service.GetCreationAvailabilityAsync(_applicationId);

        availability.CanCreate.ShouldBeFalse();
        availability.Reason.ShouldBe("Assessment:AlreadyAssigned");
    }

    [Theory]
    [InlineData(nameof(AssessmentAppService.CreateAsync))]
    [InlineData(nameof(AssessmentAppService.CloneFromAiAsync))]
    [InlineData(nameof(AssessmentAppService.GetCreationAvailabilityAsync))]
    public void Should_Require_The_Existing_Create_Assessment_Permission(string methodName)
    {
        var policies = typeof(AssessmentAppService).GetMethod(methodName)!.GetCustomAttributes<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy).ToArray();

        policies.ShouldContain(UnitySelector.Review.AssessmentReviewList.Create);
        policies.ShouldNotContain(GrantApplicationPermissions.Assessments.Start);
        if (methodName == nameof(AssessmentAppService.CloneFromAiAsync))
        {
            policies.ShouldContain(AIPermissions.Analysis.ViewScoringResult);
        }
    }

    private Task<AssessmentDto> CreateAsync(bool clone) => clone
        ? _service.CloneFromAiAsync(_aiAssessmentId)
        : _service.CreateAsync(new CreateAssessmentDto { ApplicationId = _applicationId });
}

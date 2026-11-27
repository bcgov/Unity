using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.GrantManager.GrantApplications;

public class AssessmentStatusActionPermissionTests
{
    private readonly IApplicationManager _manager = Substitute.For<IApplicationManager>();
    private readonly IApplicationRepository _repository = Substitute.For<IApplicationRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Application _application = new()
    {
        Applicant = new Applicant(),
        ApplicationStatus = new ApplicationStatus { StatusCode = GrantApplicationState.UNDER_ASSESSMENT }
    };

    [Theory]
    [InlineData(GrantApplicationAction.StartAssessment, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.CompleteAssessment, GrantApplicationPermissions.Assessments.Complete)]
    [InlineData(GrantApplicationAction.StartReview, GrantApplicationPermissions.Reviews.StartInitial)]
    [InlineData(GrantApplicationAction.CompleteReview, GrantApplicationPermissions.Reviews.CompleteInitial)]
    public async Task Should_Keep_The_Action_Visible_And_Disable_It_Without_Permission(GrantApplicationAction action, string permission)
    {
        var service = CreateService(action);

        var denied = (await service.GetActions(_applicationId)).Items.Single();
        denied.IsAuthorized.ShouldBeTrue();
        denied.IsPermitted.ShouldBeFalse();

        _permissions.IsGrantedAsync(permission).Returns(true);

        (await service.GetActions(_applicationId)).Items.Single().IsPermitted.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GrantApplicationAction.StartAssessment, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.Internal_StartAssessment, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.CompleteAssessment, GrantApplicationPermissions.Assessments.Complete)]
    [InlineData(GrantApplicationAction.StartReview, GrantApplicationPermissions.Reviews.StartInitial)]
    [InlineData(GrantApplicationAction.CompleteReview, GrantApplicationPermissions.Reviews.CompleteInitial)]
    public async Task Should_Deny_Direct_Status_Requests_Without_Permission(GrantApplicationAction action, string permission)
    {
        var service = CreateService(action);

        await Should.ThrowAsync<UnauthorizedAccessException>(() => service.TriggerAction(_applicationId, action));
        await _manager.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);

        _permissions.IsGrantedAsync(permission).Returns(true);

        await service.TriggerAction(_applicationId, action);
        await _manager.Received(1).TriggerAction(_applicationId, action, null, null);
    }

    [Theory]
    [InlineData(GrantApplicationAction.StartAssessment, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.CompleteAssessment, GrantApplicationPermissions.Assessments.Complete)]
    [InlineData(GrantApplicationAction.StartReview, GrantApplicationPermissions.Reviews.StartInitial)]
    [InlineData(GrantApplicationAction.CompleteReview, GrantApplicationPermissions.Reviews.CompleteInitial)]
    public async Task Should_Still_Respect_State_Restrictions_And_Red_Stop(GrantApplicationAction action, string permission)
    {
        var service = CreateService(action, statePermitsAction: false);
        _permissions.IsGrantedAsync(permission).Returns(true);

        (await service.GetActions(_applicationId)).Items.Single().IsPermitted.ShouldBeFalse();

        service = CreateService(action);
        _application.Applicant.RedStop = true;

        (await service.GetActions(_applicationId)).Items.Single().IsPermitted.ShouldBeFalse();
        await Should.ThrowAsync<UserFriendlyException>(() => service.TriggerAction(_applicationId, action));
        await _manager.DidNotReceiveWithAnyArgs().TriggerAction(default, default, default, default);
    }

    private GrantApplicationAppService CreateService(GrantApplicationAction action, bool statePermitsAction = true)
    {
        _repository.GetAsync(_applicationId, true).Returns(_application);
        _manager.GetActions(_applicationId).Returns(new List<ApplicationActionResultItem>
        {
            new() { ApplicationAction = action, IsPermitted = statePermitsAction }
        });
        _manager.TriggerAction(_applicationId, action, null, null).Returns(_application);
        var mapper = Substitute.For<IObjectMapper>();
        mapper.Map<List<ApplicationActionResultItem>, List<ApplicationActionDto>>(Arg.Any<List<ApplicationActionResultItem>>())
            .Returns(call => call.Arg<List<ApplicationActionResultItem>>().Select(item => new ApplicationActionDto
            {
                ApplicationAction = item.ApplicationAction,
                IsPermitted = item.IsPermitted
            }).ToList());
        mapper.Map<Application, GrantApplicationDto>(_application).Returns(new GrantApplicationDto());
        var authorization = Substitute.For<IAbpAuthorizationService>();
        authorization.CurrentPrincipal.Returns(new ClaimsPrincipal());
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(async call =>
            {
                var context = new AuthorizationHandlerContext(call.Arg<IEnumerable<IAuthorizationRequirement>>(),
                    call.Arg<ClaimsPrincipal>(), call.ArgAt<object?>(1));
                await new ApplicationAuthorizationHandler(_permissions).HandleAsync(context);
                return context.HasSucceeded ? AuthorizationResult.Success() : AuthorizationResult.Failed();
            });
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetRequiredService<IAuthorizationService>().Returns(authorization);
        lazy.LazyGetRequiredService<IAbpAuthorizationService>().Returns(authorization);
        lazy.LazyGetService<IObjectMapper>(Arg.Any<Func<IServiceProvider, object>>()).Returns(mapper);
        lazy.LazyGetRequiredService<ICurrentTenant>().Returns(Substitute.For<ICurrentTenant>());
        lazy.LazyGetRequiredService<ILocalEventBus>().Returns(Substitute.For<ILocalEventBus>());
        // Red Stop's exception message is localized by the app service.
        var localizer = Substitute.For<IStringLocalizer>();
        localizer[Arg.Any<string>()].Returns(call => new LocalizedString(call.Arg<string>(), call.Arg<string>()));
        var localizerFactory = Substitute.For<IStringLocalizerFactory>();
        localizerFactory.Create(Arg.Any<Type>()).Returns(localizer);
        lazy.LazyGetRequiredService<IStringLocalizerFactory>().Returns(localizerFactory);
        return new GrantApplicationAppService(_manager, _repository, null!, null!, null!, null!, null!, null!, null!, null!,
            Substitute.For<IFeatureChecker>(), null!, Substitute.For<IUnitOfWorkManager>())
        {
            LazyServiceProvider = lazy
        };
    }
}

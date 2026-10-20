using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Integrations.Chefs;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;
using Xunit;

namespace Unity.GrantManager.ApplicationForms;

// These tests supply their own authorization service rather than the integration suite's allow-all service.
public class ApplicationFormDeletionAuthorizationTests
{
    private readonly IRepository<ApplicationForm, Guid> _repository = Substitute.For<IRepository<ApplicationForm, Guid>>();
    private readonly IAbpAuthorizationService _authorization = Substitute.For<IAbpAuthorizationService>();

    [Fact]
    public async Task Should_DenyDeletion_WhenDeleteFormsPermissionIsMissing()
    {
        var service = CreateService();
        _authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Failed());

        await Should.ThrowAsync<AbpAuthorizationException>(() => service.DeleteAsync(GrantManagerTestData.ApplicationForm1_Id));

        await _repository.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _authorization.Received(1).AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), GrantManagerPermissions.ApplicationForms.Delete);
    }

    [Fact]
    public async Task Should_HideForm_WhenDeleteFormsPermissionIsGranted()
    {
        var service = CreateService();
        var form = new ApplicationFormSeed(GrantManagerTestData.ApplicationForm1_Id);
        _authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(AuthorizationResult.Success());
        _repository.GetAsync(form.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(form);

        await service.DeleteAsync(form.Id);

        form.IsHiddenFromList.ShouldBeTrue();
        form.IsDeleted.ShouldBeFalse();
        await _repository.Received(1).UpdateAsync(form, Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().DeleteAsync(form.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    private ApplicationFormAppService CreateService()
    {
        var lazy = Substitute.For<IAbpLazyServiceProvider>();
        lazy.LazyGetRequiredService<IAuthorizationService>().Returns(_authorization);
        lazy.LazyGetRequiredService<IAbpAuthorizationService>().Returns(_authorization);
        _authorization.CurrentPrincipal.Returns(new ClaimsPrincipal(new ClaimsIdentity("Test")));
        return new ApplicationFormAppService(
            _repository,
            Substitute.For<IStringEncryptionService>(),
            Substitute.For<IApplicationFormVersionAppService>(),
            Substitute.For<IApplicationFormVersionRepository>(),
            Substitute.For<IGrantApplicationAppService>(),
            Substitute.For<IFormsApiService>(),
            Substitute.For<IApplicationFormSubmissionRepository>())
        {
            LazyServiceProvider = lazy
        };
    }
}

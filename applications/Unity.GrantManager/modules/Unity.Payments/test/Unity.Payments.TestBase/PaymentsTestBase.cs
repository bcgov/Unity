using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Volo.Abp.Testing;
using NSubstitute;
using Volo.Abp.Features;
using Volo.Abp.Users;
using Unity.Payments.Security;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.Payments;

/* All test classes are derived from this class, directly or indirectly. */
public abstract class PaymentsTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected Guid? CurrentUserId { get; set; }

    protected PaymentsTestBase()
    {
        CurrentUserId = PaymentsTestData.User1Id;
    }

    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    protected virtual Task WithUnitOfWorkAsync(Func<Task> func)
    {
        return WithUnitOfWorkAsync(new AbpUnitOfWorkOptions(), func);
    }

    protected virtual async Task WithUnitOfWorkAsync(AbpUnitOfWorkOptions options, Func<Task> action)
    {
        using var scope = ServiceProvider.CreateScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(options);
        await action();

        await uow.CompleteAsync();
    }

    protected virtual Task<TResult> WithUnitOfWorkAsync<TResult>(Func<Task<TResult>> func)
    {
        return WithUnitOfWorkAsync(new AbpUnitOfWorkOptions(), func);
    }

    protected virtual async Task<TResult> WithUnitOfWorkAsync<TResult>(AbpUnitOfWorkOptions options, Func<Task<TResult>> func)
    {
        using var scope = ServiceProvider.CreateScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(options);
        var result = await func();
        await uow.CompleteAsync();
        return result;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        // Because some of the tests rely on the feature check, always set to true for the module tests
        var featureMock = Substitute.For<IFeatureChecker>();
        featureMock.IsEnabledAsync(Arg.Any<string>()).Returns(true);
        services.AddSingleton(featureMock);

        var externalUserLookupMock = Substitute.For<FakeExternalUserLookupServiceProvider>();
        services.AddSingleton<IExternalUserLookupServiceProvider>(externalUserLookupMock);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.Id.Returns(ci => CurrentUserId);
        services.AddSingleton(currentUser);
        
                // We add a mock of this service to satisfy the IOC without having to spin up a whole settings table
        var settingManagerMock = Substitute.For<ISettingManager>();
        // Mock required calls
        services.AddSingleton(settingManagerMock);

        var tenantRepository = Substitute.For<ITenantRepository>();
        // Mock calls
        services.AddSingleton(tenantRepository);
        base.AfterAddApplication(services);
    }
}
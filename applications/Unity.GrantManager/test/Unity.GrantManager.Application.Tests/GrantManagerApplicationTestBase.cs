using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Volo.Abp.Features;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Unity.GrantManager;

public abstract class GrantManagerApplicationTestBase : GrantManagerTestBase<GrantManagerApplicationTestModule>, IAsyncLifetime
{
    protected ICurrentUser? _currentUser;
    protected IFeatureChecker? _featureChecker;

    protected GrantManagerApplicationTestBase(ITestOutputHelper _)
    {        
    }

    public virtual async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        await Task.Delay(15);        
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = Substitute.For<ICurrentUser>();
        _featureChecker = Substitute.For<IFeatureChecker>();
        _featureChecker.IsEnabledAsync(Arg.Any<string>()).Returns(false);
        services.AddSingleton(_currentUser);
        services.AddSingleton(_featureChecker);
    }

    protected void Login(Guid userId)
    {
        _currentUser?.Id.Returns(userId);
        _currentUser?.IsAuthenticated.Returns(true);
    }

    protected void SetFeatureEnabled(string featureName, bool isEnabled)
    {
        _featureChecker?.IsEnabledAsync(featureName).Returns(isEnabled);
    }
}

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Threading.Tasks;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer(
    ordererTypeName: "XUnit.Project.Orderers.DisplayNameOrderer",
    ordererAssemblyName: "XUnit.Project")]

namespace Unity.GrantManager;

public abstract class GrantManagerApplicationTestBase : GrantManagerTestBase<GrantManagerApplicationTestModule>, IAsyncLifetime
{
    protected ICurrentUser? _currentUser;

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
        services.AddSingleton(_currentUser);
    }

    protected void Login(Guid userId)
    {
        _currentUser?.Id.Returns(userId);
        _currentUser?.IsAuthenticated.Returns(true);
    }
}

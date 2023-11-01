using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer(
    ordererTypeName: "XUnit.Project.Orderers.DisplayNameOrderer",
    ordererAssemblyName: "XUnit.Project")]

namespace Unity.GrantManager;

public abstract class GrantManagerApplicationTestBase : GrantManagerTestBase<GrantManagerApplicationTestModule>, IAsyncLifetime
{    
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
}

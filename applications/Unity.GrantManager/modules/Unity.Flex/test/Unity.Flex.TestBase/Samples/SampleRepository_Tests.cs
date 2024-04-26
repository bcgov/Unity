using System.Threading.Tasks;
using Volo.Abp.Modularity;
using Xunit;

namespace Unity.Flex.Samples;

/* Write your custom repository tests like that, in this project, as abstract classes.
 * Then inherit these abstract classes from EF Core & MongoDB test projects.
 * In this way, both database providers are tests with the same set tests.
 */
public abstract class SampleRepository_Tests<TStartupModule> : FlexTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

#pragma warning disable S125 // Sections of code should not be commented out
                            //private readonly ISampleRepository _sampleRepository;

    protected SampleRepository_Tests()
    {
        //_sampleRepository = GetRequiredService<ISampleRepository>();
    }

    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public async Task Method1Async()
#pragma warning restore S2699 // Tests should include assertions
    {
        await Task.CompletedTask;
    }
}
#pragma warning restore S125 // Sections of code should not be commented out

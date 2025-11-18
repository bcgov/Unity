using Volo.Abp.Modularity;
using Xunit.Abstractions;

namespace Unity.Reporting;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class ReportingApplicationTestBase<TStartupModule> : ReportingTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected ReportingApplicationTestBase(ITestOutputHelper _)
    {
    }
}

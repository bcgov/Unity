using Volo.Abp.Modularity;

namespace Unity.Reporting;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class ReportingApplicationTestBase<TStartupModule> : ReportingTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
}

using Volo.Abp.Modularity;

namespace Unity.Reporting;

/* Inherit from this class for your domain layer tests.
 * See SampleManager_Tests for example.
 */
public abstract class ReportingDomainTestBase<TStartupModule> : ReportingTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}

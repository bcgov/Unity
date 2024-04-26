using Volo.Abp.Modularity;

namespace Unity.Flex;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class FlexApplicationTestBase<TStartupModule> : FlexTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}

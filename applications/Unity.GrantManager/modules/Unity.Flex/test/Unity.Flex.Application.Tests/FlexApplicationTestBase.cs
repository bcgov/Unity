using Xunit.Abstractions;

namespace Unity.Flex;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class FlexApplicationTestBase : FlexTestBase<FlexApplicationTestModule>
{
    protected FlexApplicationTestBase(ITestOutputHelper _)
    {       
    }
}

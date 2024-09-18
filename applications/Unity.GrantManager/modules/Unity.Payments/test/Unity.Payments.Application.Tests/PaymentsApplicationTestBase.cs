using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Unity.Payments;

/* Inherit from this class for your application layer tests.
 * See SampleAppService_Tests for example.
 */
public abstract class PaymentsApplicationTestBase : PaymentsTestBase<PaymentsApplicationTestModule>
{

}

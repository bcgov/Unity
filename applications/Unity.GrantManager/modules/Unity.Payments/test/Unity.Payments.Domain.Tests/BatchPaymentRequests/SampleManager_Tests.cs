using Shouldly;
using Unity.Payments.Services;
using Xunit;

namespace Unity.Payments.Samples;

public class PaymentsManager_Tests : PaymentsDomainTestBase
{
    private readonly PaymentsManager _paymentsManager;

    public PaymentsManager_Tests()
    {
        _paymentsManager = GetRequiredService<PaymentsManager>();
    }

    [Fact]
    public void CanResolvePaymentsManagerService()
    {
        _paymentsManager.ShouldNotBeNull();
    }
}

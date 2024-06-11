using Shouldly;
using System.ComponentModel;
using Unity.Payments.Domain.Services;
using Xunit;

namespace Unity.Payments.BatchPaymentRequests;

[Category("Domain")]
public class PaymentsManager_Tests : PaymentsApplicationTestBase
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

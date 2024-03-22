using Unity.Payments.BatchPaymentRequests;
using Volo.Abp.Modularity;

namespace Unity.Payments.Samples;

public abstract class BatchPaymentRepository_Tests<TStartupModule> : PaymentsTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly IBatchPaymentRequestsRepository _batchPaymentsRepository;

    protected BatchPaymentRepository_Tests()
    {
        _batchPaymentsRepository = GetRequiredService<IBatchPaymentRequestsRepository>();
    }
}

using System.Threading.Tasks;
using Shouldly;
using Unity.Payments.BatchPaymentRequests;
using Xunit;

namespace Unity.Payments.Samples;

public class BatchPaymentRequestAppService_Tests : PaymentsApplicationTestBase
{
    private readonly IBatchPaymentRequestAppService _batchPaymentRequestAppService;

    public BatchPaymentRequestAppService_Tests()
    {
        _batchPaymentRequestAppService = GetRequiredService<IBatchPaymentRequestAppService>();
    }

    [Fact]
    public async Task CreateAsync()
    {
        _ = await _batchPaymentRequestAppService
            .CreateAsync(new CreateBatchPaymentRequestDto())
            .ShouldThrowAsync<System.InvalidOperationException>();        
    }
}

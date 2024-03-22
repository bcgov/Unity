using System.Threading.Tasks;
using Unity.Payments.Samples;
using Xunit;
using Shouldly;

namespace Unity.Payments.EntityFrameworkCore.Samples;

public class BatchPaymentRepository_Tests : BatchPaymentRepository_Tests<PaymentsEntityFrameworkCoreTestModule>
{
    [Fact]
    public async Task GetListAsync()
    {
        await _batchPaymentsRepository
            .GetListAsync()
            .ShouldNotThrowAsync();
    }
}



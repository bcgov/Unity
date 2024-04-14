using Shouldly;
using System.Threading.Tasks;
using Xunit;

namespace Unity.Payments.PaymentConfigurations
{
    public class PaymentConfigurationAppService_Tests : PaymentsApplicationTestBase
    {
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly IPaymentConfigurationRepository _paymentConfigurationRepository;

        public PaymentConfigurationAppService_Tests()
        {
            _paymentConfigurationAppService = GetRequiredService<IPaymentConfigurationAppService>();
            _paymentConfigurationRepository = GetRequiredService<IPaymentConfigurationRepository>();
        }

        [Fact]
        public async Task GetAsync_GetsConfiguration()
        {
            // Arrange
            var inserted = await _paymentConfigurationRepository.InsertAsync(new PaymentConfiguration(                
                paymentThreshold: 500,
                AccountCoding.Create(
                ministryClient: "1234",
                projectNumber: "1234567890",
                responsibility: "123",
                serviceLine: "123456",
                stob: "12345678")
            ), true);

            // Act
            var result = await _paymentConfigurationAppService.GetAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(inserted.Id);
        }

        [Fact]
        public async Task CreateAsync_AddsConfiguration()
        {
            // Arrange
            var createPaymentConfigurationDto = new CreatePaymentConfigurationDto()
            {
                PaymentThreshold = 500,
                MinistryClient = "1234",
                ProjectNumber = "1234567890",
                Responsibility = "123",
                ServiceLine = "123456",
                Stob = "12345678"
            };

            // Act
            var created = await _paymentConfigurationAppService.CreateAsync(createPaymentConfigurationDto);
            var result = await _paymentConfigurationRepository.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.MinistryClient.ShouldBe("1234");
            result.ProjectNumber.ShouldBe("1234567890");
            result.Responsibility.ShouldBe("123");
            result.ServiceLine.ShouldBe("123456");
            result.Stob.ShouldBe("12345678");
            result.PaymentThreshold.ShouldBe(500);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesConfiguration()
        {
            // Arrange
            _ = await _paymentConfigurationRepository.InsertAsync(new PaymentConfiguration(
                paymentThreshold: 500,
                AccountCoding.Create(
                ministryClient: "1234",                
                projectNumber: "1234567890",
                responsibility: "123",
                serviceLine: "123456",
                stob: "12345678")
            ), true);

            // Act
            var updated = await _paymentConfigurationAppService.UpdateAsync(new UpdatePaymentConfigurationDto() 
            { 
                MinistryClient = "7777",
                PaymentThreshold = 1000,
                ProjectNumber = "7777777777",
                Responsibility = "777",
                ServiceLine = "777777",
                Stob = "77777777"
            });

            var result = await _paymentConfigurationRepository.GetAsync(updated.Id);

            // Assert
            result.ShouldNotBeNull();
            result.MinistryClient.ShouldBe("7777");
            result.ProjectNumber.ShouldBe("7777777777");
            result.Responsibility.ShouldBe("777");
            result.ServiceLine.ShouldBe("777777");
            result.Stob.ShouldBe("77777777");
            result.PaymentThreshold.ShouldBe(1000);
        }
    }
}

using Shouldly;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentConfigurations;
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
        [Trait("Category", "Integration")]
        public async Task GetAsync_GetsConfiguration()
        {
            // Arrange
            var inserted = await _paymentConfigurationRepository.InsertAsync(new PaymentConfiguration(
                paymentThreshold: 500,
                paymentIdPrefix: "CGG",
                AccountCoding.Create(
                ministryClient: "0TW",
                responsibility: "51OCG",
                serviceLine: "00000",
                stob: "5717",
                projectNumber: "5100000"
                )
            ), true);
            // Act
            var result = await _paymentConfigurationAppService.GetAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(inserted.Id);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateAsync_AddsConfiguration()
        {
            // Arrange
            var createPaymentConfigurationDto = new CreatePaymentConfigurationDto()
            {
                PaymentThreshold = 500,
                MinistryClient = "0TW",
                ProjectNumber = "5100000",
                Responsibility = "51OCG",
                ServiceLine = "00000",
                Stob = "5717"
            };


            // Act
            var created = await _paymentConfigurationAppService.CreateAsync(createPaymentConfigurationDto);
            var result = await _paymentConfigurationRepository.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.MinistryClient.ShouldBe("0TW");
            result.ProjectNumber.ShouldBe("5100000");
            result.Responsibility.ShouldBe("51OCG");
            result.ServiceLine.ShouldBe("00000");
            result.Stob.ShouldBe("5717");
            result.PaymentThreshold.ShouldBe(500);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateAsync_UpdatesConfiguration()
        {
            // Arrange
            _ = await _paymentConfigurationRepository.InsertAsync(new PaymentConfiguration(
                paymentThreshold: 500,
                paymentIdPrefix: "CGG",
                AccountCoding.Create(
                    ministryClient: "0TW",
                    responsibility: "51OCG",
                    serviceLine: "00000",
                    stob: "5717",
                    projectNumber: "5100000"
                )
            ), true);

            // Act
            var updated = await _paymentConfigurationAppService.UpdateAsync(new UpdatePaymentConfigurationDto() 
            { 
                MinistryClient = "0TW",
                PaymentThreshold = 1000,
                ProjectNumber = "5200000",
                Responsibility = "51OCG",
                ServiceLine = "00000",
                Stob = "5718"
            });

            var result = await _paymentConfigurationRepository.GetAsync(updated.Id);

            // Assert
            result.ShouldNotBeNull();
            result.MinistryClient.ShouldBe("0TW");
            result.ProjectNumber.ShouldBe("5200000");
            result.Responsibility.ShouldBe("51OCG");
            result.ServiceLine.ShouldBe("00000");
            result.Stob.ShouldBe("5718");
            result.PaymentThreshold.ShouldBe(1000);
        }
    }
}

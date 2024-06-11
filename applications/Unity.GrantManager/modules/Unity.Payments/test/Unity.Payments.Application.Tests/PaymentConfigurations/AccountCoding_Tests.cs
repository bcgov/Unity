using Xunit;
using Shouldly;
using Unity.Payments.Domain.PaymentConfigurations;
using System.ComponentModel;

namespace Unity.Payments.PaymentConfigurations
{
    [Category("Domain")]
    public class AccountCoding_Tests : PaymentsApplicationTestBase
    {
        [Fact]
        public void Create_ValidParameters_ShouldNotThrow()
        {
            // Arrange
            // Act
            var accountCoding = AccountCoding.Create(
                ministryClient: "0TW",
                responsibility: "51OCG",
                serviceLine: "00000",
                stob: "5717",
                projectNumber: "5100000"
                );

            // Assert
            accountCoding.ShouldNotBeNull();
        }

        [Fact]
        public void Create_InvalidMinistryClient_ShouldThrow()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<Volo.Abp.BusinessException>(() => AccountCoding.Create(
                ministryClient: "12345", // Invalid
                projectNumber: "1234567890",
                responsibility: "123",
                serviceLine: "123456",
                stob: "12345678"));
        }

        [Fact]
        public void Create_InvalidProjectNumber_ShouldThrow()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<Volo.Abp.BusinessException>(() => AccountCoding.Create(
                ministryClient: "1234",
                projectNumber: "1234567890123", // Invalid
                responsibility: "123",
                serviceLine: "123456",
                stob: "12345678"));
        }

        [Fact]
        public void Create_InvalidReposibility_ShouldThrow()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<Volo.Abp.BusinessException>(() => AccountCoding.Create(
                ministryClient: "1234",
                projectNumber: "1234567890",
                responsibility: "1234", // Invalid
                serviceLine: "123456",
                stob: "12345678"));
        }

        [Fact]
        public void Create_InvalidServiceLine_ShouldThrow()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<Volo.Abp.BusinessException>(() => AccountCoding.Create(
                ministryClient: "1234",
                projectNumber: "1234567890",
                responsibility: "123",
                serviceLine: "1234567", // Invalid
                stob: "12345678"));
        }

        [Fact]
        public void Create_InvalidStob_ShouldThrow()
        {
            // Arrange
            // Act
            // Assert
            Assert.Throws<Volo.Abp.BusinessException>(() => AccountCoding.Create(
                ministryClient: "1234",
                projectNumber: "1234567890",
                responsibility: "123",
                serviceLine: "123456",
                stob: "123456789")); // Invalid
        }
    }
}

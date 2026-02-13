using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ApplicantProfile;
using Unity.GrantManager.Applicants.ProfileData;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantProfileAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly ApplicantProfileAppService _service;

        public ApplicantProfileAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _service = GetRequiredService<ApplicantProfileAppService>();
        }

        private static ApplicantProfileInfoRequest CreateRequest(string key) => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = key
        };

        [Theory]
        [InlineData(ApplicantProfileKeys.ContactInfo)]
        [InlineData(ApplicantProfileKeys.OrgInfo)]
        [InlineData(ApplicantProfileKeys.AddressInfo)]
        [InlineData(ApplicantProfileKeys.SubmissionInfo)]
        [InlineData(ApplicantProfileKeys.PaymentInfo)]
        public async Task GetApplicantProfileAsync_WithValidKey_ShouldReturnData(string key)
        {
            // Arrange
            var request = CreateRequest(key);

            // Act
            var result = await _service.GetApplicantProfileAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Key.ShouldBe(key);
            result.Data.ShouldNotBeNull();
            result.ProfileId.ShouldBe(request.ProfileId);
            result.Subject.ShouldBe(request.Subject);
            result.TenantId.ShouldBe(request.TenantId);
        }

        [Theory]
        [InlineData(ApplicantProfileKeys.ContactInfo, typeof(ApplicantContactInfoDto))]
        [InlineData(ApplicantProfileKeys.OrgInfo, typeof(ApplicantOrgInfoDto))]
        [InlineData(ApplicantProfileKeys.AddressInfo, typeof(ApplicantAddressInfoDto))]
        [InlineData(ApplicantProfileKeys.SubmissionInfo, typeof(ApplicantSubmissionInfoDto))]
        [InlineData(ApplicantProfileKeys.PaymentInfo, typeof(ApplicantPaymentInfoDto))]
        public async Task GetApplicantProfileAsync_WithValidKey_ShouldReturnCorrectDataType(string key, Type expectedType)
        {
            // Arrange
            var request = CreateRequest(key);

            // Act
            var result = await _service.GetApplicantProfileAsync(request);

            // Assert
            result.Data.ShouldNotBeNull();
            result.Data.ShouldBeOfType(expectedType);
        }

        [Fact]
        public async Task GetApplicantProfileAsync_WithUnknownKey_ShouldReturnNullData()
        {
            // Arrange
            var request = CreateRequest("UNKNOWNKEY");

            // Act
            var result = await _service.GetApplicantProfileAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Key.ShouldBe("UNKNOWNKEY");
            result.Data.ShouldBeNull();
        }

        [Fact]
        public async Task GetApplicantProfileAsync_KeyLookupIsCaseInsensitive()
        {
            // Arrange
            var request = CreateRequest("contactinfo");

            // Act
            var result = await _service.GetApplicantProfileAsync(request);

            // Assert
            result.Data.ShouldNotBeNull();
            result.Data.ShouldBeOfType<ApplicantContactInfoDto>();
        }

        [Fact]
        public async Task GetApplicantProfileAsync_AlwaysReturnsRequestFieldsOnDto()
        {
            // Arrange
            var request = new ApplicantProfileInfoRequest
            {
                ProfileId = Guid.NewGuid(),
                Subject = "someuser@bceid",
                TenantId = Guid.NewGuid(),
                Key = "NONEXISTENT"
            };

            // Act
            var result = await _service.GetApplicantProfileAsync(request);

            // Assert
            result.ProfileId.ShouldBe(request.ProfileId);
            result.Subject.ShouldBe(request.Subject);
            result.TenantId.ShouldBe(request.TenantId);
            result.Key.ShouldBe(request.Key);
        }
    }
}

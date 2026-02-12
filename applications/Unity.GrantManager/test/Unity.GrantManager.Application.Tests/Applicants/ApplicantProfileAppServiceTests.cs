using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ApplicantProfile;
using Unity.GrantManager.Applicants.ProfileData;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantProfileAppServiceTests
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly ITenantRepository _tenantRepository;
        private readonly IRepository<ApplicantTenantMap, Guid> _applicantTenantMapRepository;
        private readonly IRepository<ApplicationFormSubmission, Guid> _applicationFormSubmissionRepository;

        public ApplicantProfileAppServiceTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _tenantRepository = Substitute.For<ITenantRepository>();
            _applicantTenantMapRepository = Substitute.For<IRepository<ApplicantTenantMap, Guid>>();
            _applicationFormSubmissionRepository = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
        }

        private ApplicantProfileAppService CreateService(IEnumerable<IApplicantProfileDataProvider> providers)
        {
            return new ApplicantProfileAppService(
                _currentTenant,
                _tenantRepository,
                _applicantTenantMapRepository,
                _applicationFormSubmissionRepository,
                providers);
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
        public async Task GetApplicantProfileAsync_WithValidKey_ShouldReturnDataFromProvider(string key)
        {
            // Arrange
            var expectedData = Substitute.For<ApplicantProfileDataDto>();
            var mockProvider = Substitute.For<IApplicantProfileDataProvider>();
            mockProvider.Key.Returns(key);
            mockProvider.GetDataAsync(Arg.Any<ApplicantProfileInfoRequest>()).Returns(expectedData);

            var service = CreateService([mockProvider]);
            var request = CreateRequest(key);

            // Act
            var result = await service.GetApplicantProfileAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Key.ShouldBe(key);
            result.Data.ShouldBe(expectedData);
            result.ProfileId.ShouldBe(request.ProfileId);
            result.Subject.ShouldBe(request.Subject);
            result.TenantId.ShouldBe(request.TenantId);
            await mockProvider.Received(1).GetDataAsync(Arg.Is<ApplicantProfileInfoRequest>(r => r.Key == key));
        }

        [Fact]
        public async Task GetApplicantProfileAsync_WithUnknownKey_ShouldReturnNullData()
        {
            // Arrange
            var service = CreateService([]);
            var request = CreateRequest("UNKNOWNKEY");

            // Act
            var result = await service.GetApplicantProfileAsync(request);

            // Assert
            result.ShouldNotBeNull();
            result.Key.ShouldBe("UNKNOWNKEY");
            result.Data.ShouldBeNull();
        }

        [Fact]
        public async Task GetApplicantProfileAsync_KeyLookupIsCaseInsensitive()
        {
            // Arrange
            var expectedData = Substitute.For<ApplicantProfileDataDto>();
            var mockProvider = Substitute.For<IApplicantProfileDataProvider>();
            mockProvider.Key.Returns(ApplicantProfileKeys.ContactInfo);
            mockProvider.GetDataAsync(Arg.Any<ApplicantProfileInfoRequest>()).Returns(expectedData);

            var service = CreateService([mockProvider]);
            var request = CreateRequest("contactinfo");

            // Act
            var result = await service.GetApplicantProfileAsync(request);

            // Assert
            result.Data.ShouldBe(expectedData);
        }

        [Fact]
        public async Task GetApplicantProfileAsync_WithMultipleProviders_ShouldDispatchToCorrectOne()
        {
            // Arrange
            var contactData = new ApplicantContactInfoDto();
            var orgData = new ApplicantOrgInfoDto();

            var contactProvider = Substitute.For<IApplicantProfileDataProvider>();
            contactProvider.Key.Returns(ApplicantProfileKeys.ContactInfo);
            contactProvider.GetDataAsync(Arg.Any<ApplicantProfileInfoRequest>()).Returns(contactData);

            var orgProvider = Substitute.For<IApplicantProfileDataProvider>();
            orgProvider.Key.Returns(ApplicantProfileKeys.OrgInfo);
            orgProvider.GetDataAsync(Arg.Any<ApplicantProfileInfoRequest>()).Returns(orgData);

            var service = CreateService([contactProvider, orgProvider]);

            // Act
            var result = await service.GetApplicantProfileAsync(CreateRequest(ApplicantProfileKeys.OrgInfo));

            // Assert
            result.Data.ShouldBeOfType<ApplicantOrgInfoDto>();
            await contactProvider.DidNotReceive().GetDataAsync(Arg.Any<ApplicantProfileInfoRequest>());
            await orgProvider.Received(1).GetDataAsync(Arg.Any<ApplicantProfileInfoRequest>());
        }

        [Fact]
        public async Task GetApplicantProfileAsync_AlwaysReturnsRequestFieldsOnDto()
        {
            // Arrange
            var service = CreateService([]);
            var request = new ApplicantProfileInfoRequest
            {
                ProfileId = Guid.NewGuid(),
                Subject = "someuser@bceid",
                TenantId = Guid.NewGuid(),
                Key = "NONEXISTENT"
            };

            // Act
            var result = await service.GetApplicantProfileAsync(request);

            // Assert
            result.ProfileId.ShouldBe(request.ProfileId);
            result.Subject.ShouldBe(request.Subject);
            result.TenantId.ShouldBe(request.TenantId);
            result.Key.ShouldBe(request.Key);
        }
    }
}

using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.TestHelpers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantProfileDataProviderTests
    {
        private static ApplicantProfileInfoRequest CreateRequest(string key) => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = key
        };

        private static ContactInfoDataProvider CreateContactInfoDataProvider()
        {
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            var applicantProfileContactService = Substitute.For<IApplicantProfileContactService>();
            applicantProfileContactService.GetProfileContactsAsync(Arg.Any<Guid>())
                .Returns(Task.FromResult(new List<ContactInfoItemDto>()));
            applicantProfileContactService.GetApplicationContactsBySubjectAsync(Arg.Any<string>())
                .Returns(Task.FromResult(new List<ContactInfoItemDto>()));
            return new ContactInfoDataProvider(currentTenant, applicantProfileContactService);
        }

        private static SubmissionInfoDataProvider CreateSubmissionInfoDataProvider()
        {
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            var submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            submissionRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            var applicationRepo = Substitute.For<IRepository<Application, Guid>>();
            applicationRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<Application>().AsAsyncQueryable()));
            var statusRepo = Substitute.For<IRepository<ApplicationStatus, Guid>>();
            statusRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<ApplicationStatus>().AsAsyncQueryable()));
            var endpointManagementAppService = Substitute.For<IEndpointManagementAppService>();
            endpointManagementAppService.GetChefsApiBaseUrlAsync().Returns(Task.FromResult(string.Empty));
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<SubmissionInfoDataProvider>>();
            return new SubmissionInfoDataProvider(currentTenant, submissionRepo, applicationRepo, statusRepo, endpointManagementAppService, logger);
        }

        [Fact]
        public void ContactInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = CreateContactInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.ContactInfo);
        }

        [Fact]
        public async Task ContactInfoDataProvider_GetDataAsync_ShouldReturnContactInfoDto()
        {
            var provider = CreateContactInfoDataProvider();
            var result = await provider.GetDataAsync(CreateRequest(ApplicantProfileKeys.ContactInfo));
            result.ShouldNotBeNull();
            result.ShouldBeOfType<ApplicantContactInfoDto>();
        }

        [Fact]
        public void OrgInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = new OrgInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.OrgInfo);
        }

        [Fact]
        public async Task OrgInfoDataProvider_GetDataAsync_ShouldReturnOrgInfoDto()
        {
            var provider = new OrgInfoDataProvider();
            var result = await provider.GetDataAsync(CreateRequest(ApplicantProfileKeys.OrgInfo));
            result.ShouldNotBeNull();
            result.ShouldBeOfType<ApplicantOrgInfoDto>();
        }

        [Fact]
        public void AddressInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = new AddressInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.AddressInfo);
        }

        [Fact]
        public async Task AddressInfoDataProvider_GetDataAsync_ShouldReturnAddressInfoDto()
        {
            var provider = new AddressInfoDataProvider();
            var result = await provider.GetDataAsync(CreateRequest(ApplicantProfileKeys.AddressInfo));
            result.ShouldNotBeNull();
            result.ShouldBeOfType<ApplicantAddressInfoDto>();
        }

        [Fact]
        public void SubmissionInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = CreateSubmissionInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.SubmissionInfo);
        }

        [Fact]
        public async Task SubmissionInfoDataProvider_GetDataAsync_ShouldReturnSubmissionInfoDto()
        {
            var provider = CreateSubmissionInfoDataProvider();
            var result = await provider.GetDataAsync(CreateRequest(ApplicantProfileKeys.SubmissionInfo));
            result.ShouldNotBeNull();
            result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
        }

        [Fact]
        public void PaymentInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = new PaymentInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.PaymentInfo);
        }

        [Fact]
        public async Task PaymentInfoDataProvider_GetDataAsync_ShouldReturnPaymentInfoDto()
        {
            var provider = new PaymentInfoDataProvider();
            var result = await provider.GetDataAsync(CreateRequest(ApplicantProfileKeys.PaymentInfo));
            result.ShouldNotBeNull();
            result.ShouldBeOfType<ApplicantPaymentInfoDto>();
        }

        [Fact]
        public void AllProviders_ShouldHaveUniqueKeys()
        {
            IApplicantProfileDataProvider[] providers =
            [
                CreateContactInfoDataProvider(),
                new OrgInfoDataProvider(),
                new AddressInfoDataProvider(),
                CreateSubmissionInfoDataProvider(),
                new PaymentInfoDataProvider()
            ];

            var keys = providers.Select(p => p.Key).ToList();
            keys.Count.ShouldBe(keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }
}

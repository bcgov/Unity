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
using Unity.Payments.Domain.PaymentRequests;
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
            applicantProfileContactService.GetApplicantAgentContactsBySubjectAsync(Arg.Any<string>())
                .Returns(Task.FromResult(new List<ContactInfoItemDto>()));
            return new ContactInfoDataProvider(currentTenant, applicantProfileContactService);
        }

        private static AddressInfoDataProvider CreateAddressInfoDataProvider()
        {
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            var submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            submissionRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            var addressRepo = Substitute.For<IRepository<ApplicantAddress, Guid>>();
            addressRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<ApplicantAddress>().AsAsyncQueryable()));
            var applicationRepo = Substitute.For<IRepository<Application, Guid>>();
            applicationRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<Application>().AsAsyncQueryable()));
            return new AddressInfoDataProvider(currentTenant, submissionRepo, addressRepo, applicationRepo);
        }

        private static OrgInfoDataProvider CreateOrgInfoDataProvider()
        {
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            var submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            submissionRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            var applicantRepo = Substitute.For<IRepository<Applicant, Guid>>();
            applicantRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<Applicant>().AsAsyncQueryable()));
            return new OrgInfoDataProvider(currentTenant, submissionRepo, applicantRepo);
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
            var provider = CreateOrgInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.OrgInfo);
        }

        [Fact]
        public async Task OrgInfoDataProvider_GetDataAsync_ShouldReturnOrgInfoDto()
        {
            var provider = CreateOrgInfoDataProvider();
            var result = await provider.GetDataAsync(CreateRequest(ApplicantProfileKeys.OrgInfo));
            result.ShouldNotBeNull();
            result.ShouldBeOfType<ApplicantOrgInfoDto>();
        }

        [Fact]
        public void AddressInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = CreateAddressInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.AddressInfo);
        }

        [Fact]
        public async Task AddressInfoDataProvider_GetDataAsync_ShouldReturnAddressInfoDto()
        {
            var provider = CreateAddressInfoDataProvider();
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

        private static PaymentInfoDataProvider CreatePaymentInfoDataProvider()
        {
            var currentTenant = Substitute.For<ICurrentTenant>();
            currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            var submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            submissionRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            var applicationRepo = Substitute.For<IRepository<Application, Guid>>();
            applicationRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<Application>().AsAsyncQueryable()));
            var paymentRequestRepo = Substitute.For<IRepository<PaymentRequest, Guid>>();
            paymentRequestRepo.GetQueryableAsync().Returns(Task.FromResult(Enumerable.Empty<PaymentRequest>().AsAsyncQueryable()));
            return new PaymentInfoDataProvider(currentTenant, submissionRepo, applicationRepo, paymentRequestRepo);
        }

        [Fact]
        public void PaymentInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = CreatePaymentInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.PaymentInfo);
        }

        [Fact]
        public async Task PaymentInfoDataProvider_GetDataAsync_ShouldReturnPaymentInfoDto()
        {
            var provider = CreatePaymentInfoDataProvider();
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
                CreateOrgInfoDataProvider(),
                CreateAddressInfoDataProvider(),
                CreateSubmissionInfoDataProvider(),
                CreatePaymentInfoDataProvider()
            ];

            var keys = providers.Select(p => p.Key).ToList();
            keys.Count.ShouldBe(keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }
}

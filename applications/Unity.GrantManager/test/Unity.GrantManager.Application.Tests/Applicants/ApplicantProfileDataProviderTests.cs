using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ApplicantProfile;
using Unity.GrantManager.Applicants.ProfileData;
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

        [Fact]
        public void ContactInfoDataProvider_Key_ShouldMatchExpected()
        {
            var provider = new ContactInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.ContactInfo);
        }

        [Fact]
        public async Task ContactInfoDataProvider_GetDataAsync_ShouldReturnContactInfoDto()
        {
            var provider = new ContactInfoDataProvider();
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
            var provider = new SubmissionInfoDataProvider();
            provider.Key.ShouldBe(ApplicantProfileKeys.SubmissionInfo);
        }

        [Fact]
        public async Task SubmissionInfoDataProvider_GetDataAsync_ShouldReturnSubmissionInfoDto()
        {
            var provider = new SubmissionInfoDataProvider();
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
                new ContactInfoDataProvider(),
                new OrgInfoDataProvider(),
                new AddressInfoDataProvider(),
                new SubmissionInfoDataProvider(),
                new PaymentInfoDataProvider()
            ];

            var keys = providers.Select(p => p.Key).ToList();
            keys.Count.ShouldBe(keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }
}

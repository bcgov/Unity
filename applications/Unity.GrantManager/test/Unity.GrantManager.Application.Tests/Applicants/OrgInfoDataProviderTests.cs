using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.TestHelpers;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Applicants
{
    public class OrgInfoDataProviderTests
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly IRepository<ApplicationFormSubmission, Guid> _submissionRepo;
        private readonly IRepository<Applicant, Guid> _applicantRepo;
        private readonly OrgInfoDataProvider _provider;

        public OrgInfoDataProviderTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            _submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            _applicantRepo = Substitute.For<IRepository<Applicant, Guid>>();

            SetupEmptyQueryables();

            _provider = new OrgInfoDataProvider(_currentTenant, _submissionRepo, _applicantRepo);
        }

        private void SetupEmptyQueryables()
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            _applicantRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<Applicant>().AsAsyncQueryable()));
        }

        private void SetupQueryables(
            IEnumerable<ApplicationFormSubmission> submissions,
            IEnumerable<Applicant> applicants)
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(submissions.AsAsyncQueryable()));
            _applicantRepo.GetQueryableAsync()
                .Returns(Task.FromResult(applicants.AsAsyncQueryable()));
        }

        private static ApplicantProfileInfoRequest CreateRequest() => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = ApplicantProfileKeys.OrgInfo
        };

        private static ApplicationFormSubmission CreateSubmission(
            Guid applicationId, string oidcSub, Guid applicantId)
        {
            var entity = new ApplicationFormSubmission
            {
                ApplicationId = applicationId,
                OidcSub = oidcSub,
                ApplicantId = applicantId
            };
            EntityHelper.TrySetId(entity, () => Guid.NewGuid());
            return entity;
        }

        private static Applicant CreateApplicant(Guid id, Action<Applicant>? configure = null)
        {
            var entity = new Applicant();
            EntityHelper.TrySetId(entity, () => id);
            configure?.Invoke(entity);
            return entity;
        }

        [Fact]
        public void Key_ShouldMatchExpected()
        {
            _provider.Key.ShouldBe(ApplicantProfileKeys.OrgInfo);
        }

        [Fact]
        public async Task GetDataAsync_ShouldChangeTenant()
        {
            var request = CreateRequest();

            await _provider.GetDataAsync(request);

            _currentTenant.Received(1).Change(request.TenantId);
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnCorrectDataType()
        {
            var request = CreateRequest();

            var result = await _provider.GetDataAsync(request);

            result.DataType.ShouldBe("ORGINFO");
        }

        [Fact]
        public async Task GetDataAsync_WithNoSubmissions_ShouldReturnEmptyList()
        {
            var request = CreateRequest();

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldMapAllApplicantFields()
        {
            var request = CreateRequest();
            var applicantId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", applicantId)],
                [CreateApplicant(applicantId, a =>
                {
                    a.UnityApplicantId = "APP-00123";
                    a.ApplicantName = "Jane Smith";
                    a.OrgName = "Acme Corp";
                    a.OrganizationType = "Non-Profit";
                    a.OrgNumber = "BC1234567";
                    a.OrgStatus = "Active";
                    a.NonRegOrgName = "Acme Trading";
                    a.FiscalMonth = "April";
                    a.FiscalDay = 1;
                    a.OrganizationSize = "51-100";
                    a.Sector = "Technology";
                    a.SubSector = "Software";
                })]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.Count.ShouldBe(1);

            var org = dto.Organizations[0];
            org.Id.ShouldBe(applicantId);
            org.ApplicantRefId.ShouldBe("APP-00123");
            org.ApplicantName.ShouldBe("Jane Smith");
            org.OrgName.ShouldBe("Acme Corp");
            org.OrganizationType.ShouldBe("Non-Profit");
            org.OrgNumber.ShouldBe("BC1234567");
            org.OrgStatus.ShouldBe("Active");
            org.NonRegOrgName.ShouldBe("Acme Trading");
            org.FiscalMonth.ShouldBe("April");
            org.FiscalDay.ShouldBe(1);
            org.OrganizationSize.ShouldBe("51-100");
            org.Sector.ShouldBe("Technology");
            org.SubSector.ShouldBe("Software");
        }

        [Fact]
        public async Task GetDataAsync_ShouldHandleNullApplicantFields()
        {
            var request = CreateRequest();
            var applicantId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", applicantId)],
                [CreateApplicant(applicantId)]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.Count.ShouldBe(1);

            var org = dto.Organizations[0];
            org.OrgName.ShouldBeNull();
            org.ApplicantRefId.ShouldBeNull();
            org.ApplicantName.ShouldBeNull();
            org.OrganizationType.ShouldBeNull();
            org.OrgNumber.ShouldBeNull();
            org.OrgStatus.ShouldBeNull();
            org.NonRegOrgName.ShouldBeNull();
            org.FiscalMonth.ShouldBeNull();
            org.FiscalDay.ShouldBeNull();
            org.OrganizationSize.ShouldBeNull();
            org.Sector.ShouldBeNull();
            org.SubSector.ShouldBeNull();
        }

        [Fact]
        public async Task GetDataAsync_ShouldNotReturnApplicantsForOtherSubjects()
        {
            var request = CreateRequest();
            var applicantId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "OTHERUSER", applicantId)],
                [CreateApplicant(applicantId, a => a.OrgName = "Other Org")]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnMultipleApplicants()
        {
            var request = CreateRequest();
            var applicantId1 = Guid.NewGuid();
            var applicantId2 = Guid.NewGuid();
            var applicationId1 = Guid.NewGuid();
            var applicationId2 = Guid.NewGuid();

            SetupQueryables(
                [
                    CreateSubmission(applicationId1, "TESTUSER", applicantId1),
                    CreateSubmission(applicationId2, "TESTUSER", applicantId2)
                ],
                [
                    CreateApplicant(applicantId1, a => a.OrgName = "Org One"),
                    CreateApplicant(applicantId2, a => a.OrgName = "Org Two")
                ]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.Count.ShouldBe(2);
            dto.Organizations.ShouldContain(o => o.OrgName == "Org One");
            dto.Organizations.ShouldContain(o => o.OrgName == "Org Two");
        }

        [Fact]
        public async Task GetDataAsync_MultipleSubmissionsSameApplicant_ShouldReturnDistinct()
        {
            var request = CreateRequest();
            var applicantId = Guid.NewGuid();
            var applicationId1 = Guid.NewGuid();
            var applicationId2 = Guid.NewGuid();

            SetupQueryables(
                [
                    CreateSubmission(applicationId1, "TESTUSER", applicantId),
                    CreateSubmission(applicationId2, "TESTUSER", applicantId)
                ],
                [CreateApplicant(applicantId, a => a.OrgName = "Same Org")]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.Count.ShouldBe(1);
            dto.Organizations[0].OrgName.ShouldBe("Same Org");
        }

        [Fact]
        public async Task GetDataAsync_ShouldNormalizeSubjectWithAtSign()
        {
            var request = new ApplicantProfileInfoRequest
            {
                ProfileId = Guid.NewGuid(),
                Subject = "testuser@idir",
                TenantId = Guid.NewGuid(),
                Key = ApplicantProfileKeys.OrgInfo
            };

            var applicantId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", applicantId)],
                [CreateApplicant(applicantId, a => a.OrgName = "Test Org")]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetDataAsync_ShouldNormalizeSubjectWithoutAtSign()
        {
            var request = new ApplicantProfileInfoRequest
            {
                ProfileId = Guid.NewGuid(),
                Subject = "testuser",
                TenantId = Guid.NewGuid(),
                Key = ApplicantProfileKeys.OrgInfo
            };

            var applicantId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", applicantId)],
                [CreateApplicant(applicantId, a => a.OrgName = "Test Org")]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetDataAsync_WithNullSubject_ShouldReturnEmptyList()
        {
            var request = new ApplicantProfileInfoRequest
            {
                ProfileId = Guid.NewGuid(),
                Subject = null!,
                TenantId = Guid.NewGuid(),
                Key = ApplicantProfileKeys.OrgInfo
            };

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantOrgInfoDto>();
            dto.Organizations.ShouldBeEmpty();
        }
    }
}

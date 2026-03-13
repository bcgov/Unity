using Microsoft.Extensions.Logging;
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
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Applicants
{
    public class SubmissionInfoDataProviderTests
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly IRepository<ApplicationFormSubmission, Guid> _submissionRepo;
        private readonly IRepository<Application, Guid> _applicationRepo;
        private readonly IRepository<ApplicationStatus, Guid> _statusRepo;
        private readonly IEndpointManagementAppService _endpointManagementAppService;
        private readonly ILogger<SubmissionInfoDataProvider> _logger;
        private readonly SubmissionInfoDataProvider _provider;

        public SubmissionInfoDataProviderTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            _submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            _applicationRepo = Substitute.For<IRepository<Application, Guid>>();
            _statusRepo = Substitute.For<IRepository<ApplicationStatus, Guid>>();
            _endpointManagementAppService = Substitute.For<IEndpointManagementAppService>();
            _endpointManagementAppService.GetChefsApiBaseUrlAsync()
                .Returns(Task.FromResult(string.Empty));
            _logger = Substitute.For<ILogger<SubmissionInfoDataProvider>>();

            SetupEmptyQueryables();

            _provider = new SubmissionInfoDataProvider(
                _currentTenant, _submissionRepo, _applicationRepo,
                _statusRepo, _endpointManagementAppService, _logger);
        }

        private void SetupEmptyQueryables()
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            _applicationRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<Application>().AsAsyncQueryable()));
            _statusRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<ApplicationStatus>().AsAsyncQueryable()));
        }

        private void SetupQueryables(
            IEnumerable<ApplicationFormSubmission> submissions,
            IEnumerable<Application> applications,
            IEnumerable<ApplicationStatus> statuses)
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(submissions.AsAsyncQueryable()));
            _applicationRepo.GetQueryableAsync()
                .Returns(Task.FromResult(applications.AsAsyncQueryable()));
            _statusRepo.GetQueryableAsync()
                .Returns(Task.FromResult(statuses.AsAsyncQueryable()));
        }

        private static ApplicantProfileInfoRequest CreateRequest() => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = ApplicantProfileKeys.SubmissionInfo
        };

        private static ApplicationFormSubmission CreateSubmission(
            Guid applicationId, string oidcSub, Action<ApplicationFormSubmission>? configure = null)
        {
            var entity = new ApplicationFormSubmission
            {
                ApplicationId = applicationId,
                OidcSub = oidcSub,
                Submission = "{}"
            };
            EntityHelper.TrySetId(entity, () => Guid.NewGuid());
            configure?.Invoke(entity);
            return entity;
        }

        private static Application CreateApplication(Guid id, Guid statusId, Action<Application>? configure = null)
        {
            var entity = new Application { ApplicationStatusId = statusId };
            EntityHelper.TrySetId(entity, () => id);
            configure?.Invoke(entity);
            return entity;
        }

        private static ApplicationStatus CreateStatus(Guid id, string externalStatus)
        {
            var entity = new ApplicationStatus { ExternalStatus = externalStatus };
            EntityHelper.TrySetId(entity, () => id);
            return entity;
        }

        [Fact]
        public async Task GetDataAsync_ShouldChangeTenant()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            _currentTenant.Received(1).Change(request.TenantId);
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnCorrectDataType()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            result.DataType.ShouldBe("SUBMISSIONINFO");
        }

        [Fact]
        public async Task GetDataAsync_WithNoSubmissions_ShouldReturnEmptyList()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldMapSubmissionFields()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var statusId = Guid.NewGuid();
            var creationTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s =>
                {
                    s.ChefsSubmissionGuid = "abc-123";
                    s.CreationTime = creationTime;
                })],
                [CreateApplication(applicationId, statusId, a =>
                {
                    a.ReferenceNo = "REF-001";
                    a.ProjectName = "Test Project";
                })],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions.Count.ShouldBe(1);

            var sub = dto.Submissions[0];
            sub.LinkId.ShouldBe("abc-123");
            sub.ReceivedTime.ShouldBe(creationTime);
            sub.ReferenceNo.ShouldBe("REF-001");
            sub.ProjectName.ShouldBe("Test Project");
            sub.Status.ShouldBe("Submitted");
        }

        [Fact]
        public async Task GetDataAsync_ShouldResolveSubmissionTimeFromJson()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var statusId = Guid.NewGuid();
            var creationTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var chefsCreatedAt = new DateTime(2025, 1, 14, 21, 37, 52, DateTimeKind.Utc);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s =>
                {
                    s.CreationTime = creationTime;
                    s.Submission = """{"createdAt": "2025-01-14T21:37:52.000Z"}""";
                })],
                [CreateApplication(applicationId, statusId)],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions[0].SubmissionTime.ShouldBe(chefsCreatedAt);
            dto.Submissions[0].ReceivedTime.ShouldBe(creationTime);
        }

        [Fact]
        public async Task GetDataAsync_ShouldFallBackToCreationTimeWhenNoCreatedAt()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var statusId = Guid.NewGuid();
            var creationTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s =>
                {
                    s.CreationTime = creationTime;
                    s.Submission = """{"id": "some-id"}""";
                })],
                [CreateApplication(applicationId, statusId)],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions[0].SubmissionTime.ShouldBe(creationTime);
        }

        [Fact]
        public async Task GetDataAsync_ShouldFallBackToCreationTimeWhenInvalidJson()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var statusId = Guid.NewGuid();
            var creationTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s =>
                {
                    s.CreationTime = creationTime;
                    s.Submission = "not valid json";
                })],
                [CreateApplication(applicationId, statusId)],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions[0].SubmissionTime.ShouldBe(creationTime);
        }

        [Fact]
        public async Task GetDataAsync_ShouldResolveLinkSourceFromIntakeApiBase()
        {
            // Arrange
            var request = CreateRequest();
            _endpointManagementAppService.GetChefsApiBaseUrlAsync()
                .Returns(Task.FromResult("https://chefs-dev.apps.silver.devops.gov.bc.ca/app/api/v1"));

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.LinkSource.ShouldBe("https://chefs-dev.apps.silver.devops.gov.bc.ca/app/user/view?s=");
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnEmptyLinkSourceWhenSettingFails()
        {
            // Arrange
            var request = CreateRequest();
            _endpointManagementAppService.GetChefsApiBaseUrlAsync()
                .Returns<string>(x => throw new Exception("Not configured"));

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.LinkSource.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldNotReturnSubmissionsForOtherSubjects()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var statusId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "OTHERUSER")],
                [CreateApplication(applicationId, statusId)],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnMultipleSubmissions()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId1 = Guid.NewGuid();
            var applicationId2 = Guid.NewGuid();
            var statusId = Guid.NewGuid();

            SetupQueryables(
                [
                    CreateSubmission(applicationId1, "TESTUSER", s => s.ChefsSubmissionGuid = "sub-1"),
                    CreateSubmission(applicationId2, "TESTUSER", s => s.ChefsSubmissionGuid = "sub-2")
                ],
                [
                    CreateApplication(applicationId1, statusId, a => a.ReferenceNo = "REF-001"),
                    CreateApplication(applicationId2, statusId, a => a.ReferenceNo = "REF-002")
                ],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions.Count.ShouldBe(2);
            dto.Submissions.ShouldContain(s => s.ReferenceNo == "REF-001");
            dto.Submissions.ShouldContain(s => s.ReferenceNo == "REF-002");
        }

        [Fact]
        public async Task GetDataAsync_ShouldNormalizeSubjectWithoutAtSign()
        {
            // Arrange
            var request = new ApplicantProfileInfoRequest
            {
                ProfileId = Guid.NewGuid(),
                Subject = "testuser",
                TenantId = Guid.NewGuid(),
                Key = ApplicantProfileKeys.SubmissionInfo
            };
            var applicationId = Guid.NewGuid();
            var statusId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateApplication(applicationId, statusId)],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetDataAsync_ShouldResolveLinkSourceWithTrailingSlash()
        {
            // Arrange
            var request = CreateRequest();
            _endpointManagementAppService.GetChefsApiBaseUrlAsync()
                .Returns(Task.FromResult("https://chefs-dev.apps.silver.devops.gov.bc.ca/app/api/v1/"));

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.LinkSource.ShouldBe("https://chefs-dev.apps.silver.devops.gov.bc.ca/app/user/view?s=");
        }

        [Fact]
        public async Task GetDataAsync_ShouldFallBackToCreationTimeWhenSubmissionIsNullOrEmpty()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var statusId = Guid.NewGuid();
            var creationTime = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s =>
                {
                    s.CreationTime = creationTime;
                    s.Submission = null!;
                })],
                [CreateApplication(applicationId, statusId)],
                [CreateStatus(statusId, "Submitted")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantSubmissionInfoDto>();
            dto.Submissions[0].SubmissionTime.ShouldBe(creationTime);
        }
    }
}

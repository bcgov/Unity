using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Text.Json;
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
    public class SubmissionFormDataProviderTests
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly IRepository<ApplicationFormSubmission, Guid> _submissionRepo;
        private readonly IApplicationFormVersionRepository _formVersionRepo;
        private readonly ILogger<SubmissionFormDataProvider> _logger;
        private readonly SubmissionFormDataProvider _provider;

        private const string SchemaJson = """{"type":"form","display":"form","components":[]}""";
        private const string SubmissionJson = """{"createdAt":"2025-01-14T21:37:52.000Z","data":{"_ApplicantName":"Test"},"state":"submitted"}""";

        public SubmissionFormDataProviderTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            _submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            _formVersionRepo = Substitute.For<IApplicationFormVersionRepository>();
            _logger = Substitute.For<ILogger<SubmissionFormDataProvider>>();

            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));

            _provider = new SubmissionFormDataProvider(_currentTenant, _submissionRepo, _formVersionRepo, _logger);
        }

        private static ApplicantProfileInfoRequest CreateRequest(Guid? submissionId) => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = ApplicantProfileKeys.SubmissionFormData,
            SubmissionId = submissionId
        };

        private static ApplicationFormSubmission CreateSubmission(
            Guid id, string oidcSub, Action<ApplicationFormSubmission>? configure = null)
        {
            var entity = new ApplicationFormSubmission
            {
                ApplicationId = Guid.NewGuid(),
                OidcSub = oidcSub,
                Submission = SubmissionJson
            };
            EntityHelper.TrySetId(entity, () => id);
            configure?.Invoke(entity);
            return entity;
        }

        private static ApplicationFormVersion CreateFormVersion(Guid id, string? schema = SchemaJson)
        {
            var entity = new ApplicationFormVersion { FormSchema = schema };
            EntityHelper.TrySetId(entity, () => id);
            return entity;
        }

        [Fact]
        public void Key_ShouldMatchExpected()
        {
            _provider.Key.ShouldBe(ApplicantProfileKeys.SubmissionFormData);
        }

        [Fact]
        public async Task GetDataAsync_ShouldThrowEntityNotFound_WhenSubmissionIdMissing()
        {
            var request = CreateRequest(null);

            await Should.ThrowAsync<EntityNotFoundException>(() => _provider.GetDataAsync(request));
        }

        [Fact]
        public async Task GetDataAsync_ShouldThrowEntityNotFound_WhenSubmissionIdEmpty()
        {
            var request = CreateRequest(Guid.Empty);

            await Should.ThrowAsync<EntityNotFoundException>(() => _provider.GetDataAsync(request));
        }

        [Fact]
        public async Task GetDataAsync_ShouldThrowEntityNotFound_WhenSubmissionDoesNotExist()
        {
            var request = CreateRequest(Guid.NewGuid());

            await Should.ThrowAsync<EntityNotFoundException>(() => _provider.GetDataAsync(request));
        }

        [Fact]
        public async Task GetDataAsync_ShouldThrowEntityNotFound_WhenSubmissionBelongsToAnotherSubject()
        {
            var submissionId = Guid.NewGuid();
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(new[] { CreateSubmission(submissionId, "OTHERUSER") }.AsAsyncQueryable()));

            var request = CreateRequest(submissionId);

            await Should.ThrowAsync<EntityNotFoundException>(() => _provider.GetDataAsync(request));
        }

        [Fact]
        public async Task GetDataAsync_ShouldThrowEntityNotFound_WhenNoFormVersionResolved()
        {
            var submissionId = Guid.NewGuid();
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(new[] { CreateSubmission(submissionId, "TESTUSER") }.AsAsyncQueryable()));
            _formVersionRepo.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<ApplicationFormVersion?>(null));
            _formVersionRepo.GetByChefsFormVersionAsync(Arg.Any<Guid>())
                .Returns(Task.FromResult<ApplicationFormVersion?>(null));

            var request = CreateRequest(submissionId);

            await Should.ThrowAsync<EntityNotFoundException>(() => _provider.GetDataAsync(request));
        }

        [Fact]
        public async Task GetDataAsync_ShouldThrowEntityNotFound_WhenFormSchemaIsEmpty()
        {
            var submissionId = Guid.NewGuid();
            var formVersionId = Guid.NewGuid();
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(new[]
                {
                    CreateSubmission(submissionId, "TESTUSER", s => s.ApplicationFormVersionId = formVersionId)
                }.AsAsyncQueryable()));
            _formVersionRepo.FindAsync(formVersionId, Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<ApplicationFormVersion?>(CreateFormVersion(formVersionId, schema: null)));

            var request = CreateRequest(submissionId);

            await Should.ThrowAsync<EntityNotFoundException>(() => _provider.GetDataAsync(request));
        }

        [Fact]
        public async Task GetDataAsync_ShouldThrowEntityNotFound_WhenSubmissionHasNoDataProperty()
        {
            var submissionId = Guid.NewGuid();
            var formVersionId = Guid.NewGuid();
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(new[]
                {
                    CreateSubmission(submissionId, "TESTUSER", s =>
                    {
                        s.ApplicationFormVersionId = formVersionId;
                        s.Submission = """{"id":"some-id"}""";
                    })
                }.AsAsyncQueryable()));
            _formVersionRepo.FindAsync(formVersionId, Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<ApplicationFormVersion?>(CreateFormVersion(formVersionId)));

            var request = CreateRequest(submissionId);

            await Should.ThrowAsync<EntityNotFoundException>(() => _provider.GetDataAsync(request));
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnSchemaAndData_WhenResolvedByApplicationFormVersionId()
        {
            var submissionId = Guid.NewGuid();
            var formVersionId = Guid.NewGuid();
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(new[]
                {
                    CreateSubmission(submissionId, "TESTUSER", s => s.ApplicationFormVersionId = formVersionId)
                }.AsAsyncQueryable()));
            _formVersionRepo.FindAsync(formVersionId, Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns(Task.FromResult<ApplicationFormVersion?>(CreateFormVersion(formVersionId)));

            var request = CreateRequest(submissionId);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantSubmissionFormDataDto>();
            dto.DataType.ShouldBe("SUBMISSIONFORMDATA");
            dto.Schema.GetProperty("type").GetString().ShouldBe("form");
            dto.Data.GetProperty("data").GetProperty("_ApplicantName").GetString().ShouldBe("Test");
            dto.Data.GetProperty("state").GetString().ShouldBe("submitted");
        }

        [Fact]
        public async Task GetDataAsync_ShouldFallBackToChefsFormVersionGuid_WhenApplicationFormVersionIdUnresolved()
        {
            var submissionId = Guid.NewGuid();
            var chefsFormVersionId = Guid.NewGuid();
            var localFormVersionId = Guid.NewGuid();
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(new[]
                {
                    CreateSubmission(submissionId, "TESTUSER", s => s.FormVersionId = chefsFormVersionId)
                }.AsAsyncQueryable()));
            _formVersionRepo.GetByChefsFormVersionAsync(chefsFormVersionId)
                .Returns(Task.FromResult<ApplicationFormVersion?>(CreateFormVersion(localFormVersionId)));

            var request = CreateRequest(submissionId);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantSubmissionFormDataDto>();
            dto.Schema.GetProperty("type").GetString().ShouldBe("form");
        }

        [Fact]
        public async Task GetDataAsync_ShouldChangeTenant()
        {
            var request = CreateRequest(Guid.NewGuid());

            try
            {
                await _provider.GetDataAsync(request);
            }
            catch (EntityNotFoundException)
            {
                // expected: no matching submission set up for this test
            }

            _currentTenant.Received(1).Change(request.TenantId);
        }
    }
}

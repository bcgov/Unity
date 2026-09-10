using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.TestHelpers;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Applicants
{
    public class AddressInfoDataProviderTests
    {
        /// <summary>
        /// Primary inference falls back to the most recently created address of a type, so only
        /// the relative order of these matters — they are named for the role they play.
        /// </summary>
        private static readonly DateTime Older = new(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Newer = new(2023, 6, 15, 14, 30, 0, DateTimeKind.Utc);

        private readonly ICurrentTenant _currentTenant;
        private readonly IRepository<ApplicationFormSubmission, Guid> _submissionRepo;
        private readonly IRepository<ApplicantAddress, Guid> _addressRepo;
        private readonly IRepository<Application, Guid> _applicationRepo;
        private readonly AddressInfoDataProvider _provider;

        public AddressInfoDataProviderTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            _submissionRepo = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            _addressRepo = Substitute.For<IRepository<ApplicantAddress, Guid>>();
            _applicationRepo = Substitute.For<IRepository<Application, Guid>>();

            SetupEmptyQueryables();

            _provider = new AddressInfoDataProvider(_currentTenant, _submissionRepo, _addressRepo, _applicationRepo);
        }

        private void SetupEmptyQueryables()
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<ApplicationFormSubmission>().AsAsyncQueryable()));
            _addressRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<ApplicantAddress>().AsAsyncQueryable()));
            _applicationRepo.GetQueryableAsync()
                .Returns(Task.FromResult(Enumerable.Empty<Application>().AsAsyncQueryable()));
        }

        private void SetupQueryables(
            IEnumerable<ApplicationFormSubmission> submissions,
            IEnumerable<ApplicantAddress> addresses,
            IEnumerable<Application>? applications = null)
        {
            _submissionRepo.GetQueryableAsync()
                .Returns(Task.FromResult(submissions.AsAsyncQueryable()));
            _addressRepo.GetQueryableAsync()
                .Returns(Task.FromResult(addresses.AsAsyncQueryable()));
            _applicationRepo.GetQueryableAsync()
                .Returns(Task.FromResult((applications ?? []).AsAsyncQueryable()));
        }

        private static ApplicantProfileInfoRequest CreateRequest() => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = ApplicantProfileKeys.AddressInfo
        };

        private static ApplicationFormSubmission CreateSubmission(
            Guid applicationId, string oidcSub, Action<ApplicationFormSubmission>? configure = null)
        {
            var entity = new ApplicationFormSubmission { ApplicationId = applicationId, OidcSub = oidcSub };
            EntityHelper.TrySetId(entity, () => Guid.NewGuid());
            configure?.Invoke(entity);
            return entity;
        }

        private static ApplicantAddress CreateAddress(Action<ApplicantAddress> configure)
        {
            var entity = new ApplicantAddress();
            EntityHelper.TrySetId(entity, () => Guid.NewGuid());
            configure(entity);
            return entity;
        }

        /// <summary>
        /// Builds an application-scoped address from just the fields these tests vary:
        /// address type, city (used as the identifying label in assertions), creation time
        /// and the primary flag. Use the <see cref="Action{T}"/> overload for anything else.
        /// </summary>
        private static ApplicantAddress CreateAddress(
            Guid applicationId,
            AddressType addressType = AddressType.PhysicalAddress,
            string? city = null,
            DateTime? creationTime = null,
            bool isPrimary = false)
        {
            return CreateAddress(a =>
            {
                a.ApplicationId = applicationId;
                a.AddressType = addressType;
                a.City = city;

                if (creationTime.HasValue)
                {
                    a.CreationTime = creationTime.Value;
                }

                if (isPrimary)
                {
                    a.SetProperty(AddressExtraPropertyNames.IsPrimary, true);
                }
            });
        }

        private static Application CreateApplication(Guid id, Action<Application>? configure = null)
        {
            var entity = new Application();
            EntityHelper.TrySetId(entity, () => id);
            configure?.Invoke(entity);
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
            result.DataType.ShouldBe("ADDRESSINFO");
        }

        [Fact]
        public async Task GetDataAsync_WithNoAddresses_ShouldReturnEmptyList()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldMapAddressFields()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateAddress(a =>
                {
                    a.ApplicationId = applicationId;
                    a.Street = "123 Main St";
                    a.Street2 = "Suite 100";
                    a.Unit = "4A";
                    a.City = "Victoria";
                    a.Province = "BC";
                    a.Postal = "V8W 1A1";
                    a.Country = "Canada";
                    a.AddressType = AddressType.PhysicalAddress;
                })],
                [CreateApplication(applicationId, a => a.ReferenceNo = "REF-001")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(1);

            var address = dto.Addresses[0];
            address.Street.ShouldBe("123 Main St");
            address.Street2.ShouldBe("Suite 100");
            address.Unit.ShouldBe("4A");
            address.City.ShouldBe("Victoria");
            address.Province.ShouldBe("BC");
            address.PostalCode.ShouldBe("V8W 1A1");
            address.Country.ShouldBe("Canada");
            address.AddressType.ShouldBe("Physical");
            address.ReferenceNo.ShouldBe("REF-001");
            address.IsEditable.ShouldBeFalse();
        }

        [Theory]
        [InlineData(AddressType.PhysicalAddress, "Physical")]
        [InlineData(AddressType.MailingAddress, "Mailing")]
        [InlineData(AddressType.BusinessAddress, "Business")]
        public async Task GetDataAsync_ShouldMapAddressTypeName(AddressType addressType, string expectedName)
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateAddress(applicationId, addressType)],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses[0].AddressType.ShouldBe(expectedName);
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnMultipleAddressesForSameSubmission()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [
                    CreateAddress(applicationId, AddressType.PhysicalAddress, "Victoria"),
                    CreateAddress(applicationId, AddressType.MailingAddress, "Vancouver")
                ],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetDataAsync_ShouldNotReturnAddressesForOtherSubjects()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "OTHERUSER")],
                [CreateAddress(applicationId, city: "Victoria")],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ShouldHandleNullAddressFields()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateAddress(a =>
                {
                    a.ApplicationId = applicationId;
                    a.Street = null;
                    a.Street2 = null;
                    a.Unit = null;
                    a.City = null;
                    a.Province = null;
                    a.Postal = null;
                    a.Country = null;
                })],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            var address = dto.Addresses[0];
            address.Street.ShouldBe(string.Empty);
            address.Street2.ShouldBe(string.Empty);
            address.Unit.ShouldBe(string.Empty);
            address.City.ShouldBe(string.Empty);
            address.Province.ShouldBe(string.Empty);
            address.PostalCode.ShouldBe(string.Empty);
            address.Country.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnAddressesLinkedByApplicantId()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s => s.ApplicantId = applicantId)],
                [CreateAddress(a =>
                {
                    a.ApplicantId = applicantId;
                    a.City = "Kelowna";
                    a.AddressType = AddressType.MailingAddress;
                })]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(1);
            dto.Addresses[0].City.ShouldBe("Kelowna");
            dto.Addresses[0].ReferenceNo.ShouldBeNull();
            dto.Addresses[0].IsEditable.ShouldBeTrue();
        }

        [Fact]
        public async Task GetDataAsync_ShouldCombineAddressesFromBothLinks()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s => s.ApplicantId = applicantId)],
                [
                    CreateAddress(applicationId, city: "Victoria"),
                    CreateAddress(a => { a.ApplicantId = applicantId; a.City = "Kelowna"; })
                ],
                [CreateApplication(applicationId, a => a.ReferenceNo = "REF-002")]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(2);
        }

        [Fact]
        public async Task GetDataAsync_ShouldDeduplicateAddressesMatchingBothLinks()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var applicantId = Guid.NewGuid();
            var addressId = Guid.NewGuid();

            // Same address linked by both ApplicationId and ApplicantId
            var sharedAddress = new ApplicantAddress
            {
                ApplicationId = applicationId,
                ApplicantId = applicantId,
                City = "Victoria"
            };
            EntityHelper.TrySetId(sharedAddress, () => addressId);

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER", s => s.ApplicantId = applicantId)],
                [sharedAddress],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert — deduplicated to one entry, application-linked (not editable) wins
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(1);
            dto.Addresses[0].City.ShouldBe("Victoria");
            dto.Addresses[0].IsEditable.ShouldBeFalse();
        }

        [Fact]
        public async Task GetDataAsync_MultipleApplicantIds_ShouldMakeApplicantPathNotEditable()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId1 = Guid.NewGuid();
            var applicationId2 = Guid.NewGuid();
            var applicantId1 = Guid.NewGuid();
            var applicantId2 = Guid.NewGuid();

            SetupQueryables(
                [
                    CreateSubmission(applicationId1, "TESTUSER", s => s.ApplicantId = applicantId1),
                    CreateSubmission(applicationId2, "TESTUSER", s => s.ApplicantId = applicantId2)
                ],
                [
                    CreateAddress(a => { a.ApplicantId = applicantId1; a.City = "Victoria"; }),
                    CreateAddress(a => { a.ApplicantId = applicantId2; a.City = "Vancouver"; })
                ]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert — multiple distinct ApplicantIds means applicant-path addresses are NOT editable
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(2);
            dto.Addresses.ShouldAllBe(a => !a.IsEditable);
        }

        [Fact]
        public async Task GetDataAsync_ShouldInferOnePrimaryPerAddressTypeWhenNoneMarked()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            // No address is flagged, so each type group must infer its own most recent one.
            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [
                    CreateAddress(applicationId, AddressType.PhysicalAddress, "Vancouver", Older),
                    CreateAddress(applicationId, AddressType.PhysicalAddress, "Victoria", Newer),
                    CreateAddress(applicationId, AddressType.MailingAddress, "Nanaimo", Older),
                    CreateAddress(applicationId, AddressType.MailingAddress, "Kelowna", Newer)
                ],
                [CreateApplication(applicationId)]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(4);
            dto.Addresses.Count(a => a.IsPrimary).ShouldBe(2);
            dto.Addresses.Single(a => a.AddressType == "Physical" && a.IsPrimary).City.ShouldBe("Victoria");
            dto.Addresses.Single(a => a.AddressType == "Mailing" && a.IsPrimary).City.ShouldBe("Kelowna");
        }

        [Fact]
        public async Task GetDataAsync_ShouldOnlyInferPrimaryForTypeGroupsWithoutOne()
        {
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();

            // Vancouver is flagged but is the OLDER Physical address: asserting it below only
            // proves anything because inference, if it ran for this group, would pick Victoria.
            // Mailing has nothing flagged, so that group must still infer.
            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [
                    CreateAddress(applicationId, AddressType.PhysicalAddress, "Vancouver", Older, isPrimary: true),
                    CreateAddress(applicationId, AddressType.PhysicalAddress, "Victoria", Newer),
                    CreateAddress(applicationId, AddressType.MailingAddress, "Kelowna", Older)
                ],
                [CreateApplication(applicationId)]);

            var result = await _provider.GetDataAsync(request);

            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(3);
            dto.Addresses.Count(a => a.IsPrimary).ShouldBe(2);
            dto.Addresses.Single(a => a.AddressType == "Physical" && a.IsPrimary).City.ShouldBe("Vancouver");
            dto.Addresses.Single(a => a.AddressType == "Mailing" && a.IsPrimary).City.ShouldBe("Kelowna");
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
                Key = ApplicantProfileKeys.AddressInfo
            };
            var applicationId = Guid.NewGuid();

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [CreateAddress(applicationId, city: "Victoria")],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(1);
        }
    }
}

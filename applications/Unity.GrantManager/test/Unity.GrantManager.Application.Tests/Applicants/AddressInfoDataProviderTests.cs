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
                [CreateAddress(a => { a.ApplicationId = applicationId; a.AddressType = addressType; })],
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
                    CreateAddress(a => { a.ApplicationId = applicationId; a.AddressType = AddressType.PhysicalAddress; a.City = "Victoria"; }),
                    CreateAddress(a => { a.ApplicationId = applicationId; a.AddressType = AddressType.MailingAddress; a.City = "Vancouver"; })
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
                [CreateAddress(a => { a.ApplicationId = applicationId; a.City = "Victoria"; })],
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
                    CreateAddress(a => { a.ApplicationId = applicationId; a.City = "Victoria"; }),
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
        public async Task GetDataAsync_ShouldMarkMostRecentAddressAsPrimaryWhenNoneMarked()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var oldAddress = CreateAddress(a =>
            {
                a.ApplicationId = applicationId;
                a.City = "Vancouver";
                a.CreationTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            });
            var recentAddress = CreateAddress(a =>
            {
                a.ApplicationId = applicationId;
                a.City = "Victoria";
                a.CreationTime = new DateTime(2023, 6, 15, 14, 30, 0, DateTimeKind.Utc);
            });

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [oldAddress, recentAddress],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(2);
            var primary = dto.Addresses.Single(a => a.IsPrimary);
            primary.City.ShouldBe("Victoria");
        }

        [Fact]
        public async Task GetDataAsync_ShouldNotOverridePrimaryWhenAlreadySet()
        {
            // Arrange
            var request = CreateRequest();
            var applicationId = Guid.NewGuid();
            var primaryAddress = CreateAddress(a =>
            {
                a.ApplicationId = applicationId;
                a.City = "Vancouver";
                a.CreationTime = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
                a.SetProperty("isPrimary", true);
            });
            var recentAddress = CreateAddress(a =>
            {
                a.ApplicationId = applicationId;
                a.City = "Victoria";
                a.CreationTime = new DateTime(2023, 6, 15, 14, 30, 0, DateTimeKind.Utc);
            });

            SetupQueryables(
                [CreateSubmission(applicationId, "TESTUSER")],
                [primaryAddress, recentAddress],
                [CreateApplication(applicationId)]);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantAddressInfoDto>();
            dto.Addresses.Count.ShouldBe(2);
            var primary = dto.Addresses.Single(a => a.IsPrimary);
            primary.City.ShouldBe("Vancouver");
        }
    }
}

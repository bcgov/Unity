using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace Unity.GrantManager.Contacts
{
    public class ContactInfoDataProviderTests
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly IApplicantContactQueryService _applicantContactQueryService;
        private readonly ContactInfoDataProvider _provider;

        public ContactInfoDataProviderTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            _applicantContactQueryService = Substitute.For<IApplicantContactQueryService>();

            _applicantContactQueryService.GetApplicantContactsAsync(Arg.Any<string>())
                .Returns([]);
            _applicantContactQueryService.GetApplicationContactsBySubjectAsync(Arg.Any<string>())
                .Returns([]);
            _applicantContactQueryService.GetApplicantAgentContactsBySubjectAsync(Arg.Any<string>())
                .Returns([]);

            _provider = new ContactInfoDataProvider(_currentTenant, _applicantContactQueryService);
        }

        private static ApplicantProfileInfoRequest CreateRequest() => new()
        {
            ProfileId = Guid.NewGuid(),
            Subject = "testuser@idir",
            TenantId = Guid.NewGuid(),
            Key = ApplicantProfileKeys.ContactInfo
        };

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
        public async Task GetDataAsync_ShouldCallGetApplicantContactsWithNormalizedSubject()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            await _applicantContactQueryService.Received(1).GetApplicantContactsAsync("TESTUSER");
        }

        [Fact]
        public async Task GetDataAsync_ShouldCallGetApplicationContactsWithSubject()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            await _applicantContactQueryService.Received(1).GetApplicationContactsBySubjectAsync("TESTUSER");
        }

        [Fact]
        public async Task GetDataAsync_ShouldCallGetApplicantAgentContactsWithSubject()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            await _applicantContactQueryService.Received(1).GetApplicantAgentContactsBySubjectAsync("TESTUSER");
        }

        [Fact]
        public async Task GetDataAsync_ShouldCombineAllContactSets()
        {
            // Arrange
            var request = CreateRequest();
            var applicantContacts = new List<ContactInfoItemDto>
            {
                new() { ContactId = Guid.NewGuid(), Name = "Applicant Contact 1", IsEditable = true },
                new() { ContactId = Guid.NewGuid(), Name = "Applicant Contact 2", IsEditable = true }
            };
            var appContacts = new List<ContactInfoItemDto>
            {
                new() { ContactId = Guid.NewGuid(), Name = "App Contact 1", IsEditable = false }
            };
            var agentContacts = new List<ContactInfoItemDto>
            {
                new() { ContactId = Guid.NewGuid(), Name = "Agent Contact 1", IsEditable = false, ContactType = "ApplicantAgent" }
            };
            _applicantContactQueryService.GetApplicantContactsAsync("TESTUSER").Returns(applicantContacts);
            _applicantContactQueryService.GetApplicationContactsBySubjectAsync("TESTUSER").Returns(appContacts);
            _applicantContactQueryService.GetApplicantAgentContactsBySubjectAsync("TESTUSER").Returns(agentContacts);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts.Count.ShouldBe(4);
            dto.Contacts.Count(c => c.IsEditable).ShouldBe(2);
            dto.Contacts.Count(c => !c.IsEditable).ShouldBe(2);
        }

        [Fact]
        public async Task GetDataAsync_WithNoContacts_ShouldReturnEmptyList()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ContactsShouldAppearInExpectedOrder()
        {
            // Arrange
            var request = CreateRequest();
            var applicantContact = new ContactInfoItemDto
            {
                ContactId = Guid.NewGuid(),
                Name = "Applicant First",
                IsEditable = true
            };
            var appContact = new ContactInfoItemDto
            {
                ContactId = Guid.NewGuid(),
                Name = "App Second",
                IsEditable = false
            };
            var agentContact = new ContactInfoItemDto
            {
                ContactId = Guid.NewGuid(),
                Name = "Agent Third",
                IsEditable = false,
                ContactType = "ApplicantAgent"
            };
            _applicantContactQueryService.GetApplicantContactsAsync("TESTUSER")
                .Returns(new List<ContactInfoItemDto> { applicantContact });
            _applicantContactQueryService.GetApplicationContactsBySubjectAsync("TESTUSER")
                .Returns(new List<ContactInfoItemDto> { appContact });
            _applicantContactQueryService.GetApplicantAgentContactsBySubjectAsync("TESTUSER")
                .Returns(new List<ContactInfoItemDto> { agentContact });

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts[0].Name.ShouldBe("Applicant First");
            dto.Contacts[1].Name.ShouldBe("App Second");
            dto.Contacts[2].Name.ShouldBe("Agent Third");
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnCorrectDataType()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            result.DataType.ShouldBe("CONTACTINFO");
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
                Key = ApplicantProfileKeys.ContactInfo
            };

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            await _applicantContactQueryService.Received(1).GetApplicationContactsBySubjectAsync("TESTUSER");
            await _applicantContactQueryService.Received(1).GetApplicantAgentContactsBySubjectAsync("TESTUSER");
        }

        [Fact]
        public async Task GetDataAsync_NoPrimary_ShouldMarkLatestByCreationTimeAsPrimary()
        {
            // Arrange
            var request = CreateRequest();
            var contacts = new List<ContactInfoItemDto>
            {
                new() { ContactId = Guid.NewGuid(), Name = "Older", IsPrimary = false, CreationTime = new DateTime(2024, 1, 1) },
                new() { ContactId = Guid.NewGuid(), Name = "Newest", IsPrimary = false, CreationTime = new DateTime(2024, 6, 15) },
                new() { ContactId = Guid.NewGuid(), Name = "Middle", IsPrimary = false, CreationTime = new DateTime(2024, 3, 10) }
            };
            _applicantContactQueryService.GetApplicantContactsAsync("TESTUSER").Returns(contacts);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts.Count(c => c.IsPrimary).ShouldBe(1);
            var primary = dto.Contacts.First(c => c.IsPrimary);
            primary.Name.ShouldBe("Newest");
            primary.IsPrimaryInferred.ShouldBeTrue();
        }

        [Fact]
        public async Task GetDataAsync_ExistingPrimary_ShouldNotChangePrimary()
        {
            // Arrange
            var request = CreateRequest();
            var contacts = new List<ContactInfoItemDto>
            {
                new() { ContactId = Guid.NewGuid(), Name = "Already Primary", IsPrimary = true, IsPrimaryInferred = false, CreationTime = new DateTime(2024, 1, 1) },
                new() { ContactId = Guid.NewGuid(), Name = "Newer But Not Primary", IsPrimary = false, CreationTime = new DateTime(2024, 6, 15) }
            };
            _applicantContactQueryService.GetApplicantContactsAsync("TESTUSER").Returns(contacts);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts.Count(c => c.IsPrimary).ShouldBe(1);
            var primary = dto.Contacts.First(c => c.IsPrimary);
            primary.Name.ShouldBe("Already Primary");
            primary.IsPrimaryInferred.ShouldBeFalse();
        }

        [Fact]
        public async Task GetDataAsync_EmptyContacts_ShouldNotSetPrimary()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts.ShouldBeEmpty();
            dto.Contacts.Any(c => c.IsPrimary).ShouldBeFalse();
        }
    }
}

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
        private readonly IApplicantProfileContactService _applicantProfileContactService;
        private readonly ContactInfoDataProvider _provider;

        public ContactInfoDataProviderTests()
        {
            _currentTenant = Substitute.For<ICurrentTenant>();
            _currentTenant.Change(Arg.Any<Guid?>()).Returns(Substitute.For<IDisposable>());
            _applicantProfileContactService = Substitute.For<IApplicantProfileContactService>();
            _provider = new ContactInfoDataProvider(_currentTenant, _applicantProfileContactService);
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
            _applicantProfileContactService.GetProfileContactsAsync(Arg.Any<Guid>())
                .Returns(new List<ContactInfoItemDto>());
            _applicantProfileContactService.GetApplicationContactsBySubjectAsync(Arg.Any<string>())
                .Returns(new List<ContactInfoItemDto>());

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            _currentTenant.Received(1).Change(request.TenantId);
        }

        [Fact]
        public async Task GetDataAsync_ShouldCallGetProfileContactsWithProfileId()
        {
            // Arrange
            var request = CreateRequest();
            _applicantProfileContactService.GetProfileContactsAsync(Arg.Any<Guid>())
                .Returns(new List<ContactInfoItemDto>());
            _applicantProfileContactService.GetApplicationContactsBySubjectAsync(Arg.Any<string>())
                .Returns(new List<ContactInfoItemDto>());

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            await _applicantProfileContactService.Received(1).GetProfileContactsAsync(request.ProfileId);
        }

        [Fact]
        public async Task GetDataAsync_ShouldCallGetApplicationContactsWithSubject()
        {
            // Arrange
            var request = CreateRequest();
            _applicantProfileContactService.GetProfileContactsAsync(Arg.Any<Guid>())
                .Returns(new List<ContactInfoItemDto>());
            _applicantProfileContactService.GetApplicationContactsBySubjectAsync(Arg.Any<string>())
                .Returns(new List<ContactInfoItemDto>());

            // Act
            await _provider.GetDataAsync(request);

            // Assert
            await _applicantProfileContactService.Received(1).GetApplicationContactsBySubjectAsync(request.Subject);
        }

        [Fact]
        public async Task GetDataAsync_ShouldCombineBothContactSets()
        {
            // Arrange
            var request = CreateRequest();
            var profileContacts = new List<ContactInfoItemDto>
            {
                new() { ContactId = Guid.NewGuid(), Name = "Profile Contact 1", IsEditable = true },
                new() { ContactId = Guid.NewGuid(), Name = "Profile Contact 2", IsEditable = true }
            };
            var appContacts = new List<ContactInfoItemDto>
            {
                new() { ContactId = Guid.NewGuid(), Name = "App Contact 1", IsEditable = false }
            };
            _applicantProfileContactService.GetProfileContactsAsync(request.ProfileId).Returns(profileContacts);
            _applicantProfileContactService.GetApplicationContactsBySubjectAsync(request.Subject).Returns(appContacts);

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts.Count.ShouldBe(3);
            dto.Contacts.Count(c => c.IsEditable).ShouldBe(2);
            dto.Contacts.Count(c => !c.IsEditable).ShouldBe(1);
        }

        [Fact]
        public async Task GetDataAsync_WithNoContacts_ShouldReturnEmptyList()
        {
            // Arrange
            var request = CreateRequest();
            _applicantProfileContactService.GetProfileContactsAsync(Arg.Any<Guid>())
                .Returns(new List<ContactInfoItemDto>());
            _applicantProfileContactService.GetApplicationContactsBySubjectAsync(Arg.Any<string>())
                .Returns(new List<ContactInfoItemDto>());

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDataAsync_ProfileContactsShouldAppearBeforeApplicationContacts()
        {
            // Arrange
            var request = CreateRequest();
            var profileContact = new ContactInfoItemDto
            {
                ContactId = Guid.NewGuid(),
                Name = "Profile First",
                IsEditable = true
            };
            var appContact = new ContactInfoItemDto
            {
                ContactId = Guid.NewGuid(),
                Name = "App Second",
                IsEditable = false
            };
            _applicantProfileContactService.GetProfileContactsAsync(request.ProfileId)
                .Returns(new List<ContactInfoItemDto> { profileContact });
            _applicantProfileContactService.GetApplicationContactsBySubjectAsync(request.Subject)
                .Returns(new List<ContactInfoItemDto> { appContact });

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            var dto = result.ShouldBeOfType<ApplicantContactInfoDto>();
            dto.Contacts[0].Name.ShouldBe("Profile First");
            dto.Contacts[1].Name.ShouldBe("App Second");
        }

        [Fact]
        public async Task GetDataAsync_ShouldReturnCorrectDataType()
        {
            // Arrange
            var request = CreateRequest();
            _applicantProfileContactService.GetProfileContactsAsync(Arg.Any<Guid>())
                .Returns(new List<ContactInfoItemDto>());
            _applicantProfileContactService.GetApplicationContactsBySubjectAsync(Arg.Any<string>())
                .Returns(new List<ContactInfoItemDto>());

            // Act
            var result = await _provider.GetDataAsync(request);

            // Assert
            result.DataType.ShouldBe("CONTACTINFO");
        }
    }
}

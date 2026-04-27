using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Contacts;
using Xunit;

namespace Unity.GrantManager.ApplicantProfile
{
    public class ApplicantContactAppServiceTests
    {
        private readonly IApplicantContactQueryService _applicantContactQueryService;
        private readonly IContactManager _contactManager;
        private readonly ApplicantContactAppService _service;

        public ApplicantContactAppServiceTests()
        {
            _applicantContactQueryService = Substitute.For<IApplicantContactQueryService>();
            _contactManager = Substitute.For<IContactManager>();

            _service = new ApplicantContactAppService(
                _applicantContactQueryService,
                _contactManager);
        }

        [Fact]
        public async Task GetByApplicantIdAsync_ShouldDelegateToProfileContactService()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var expected = new ApplicantContactInfoDto { Contacts = [] };
            _applicantContactQueryService.GetByApplicantIdAsync(applicantId).Returns(expected);

            // Act
            var result = await _service.GetByApplicantIdAsync(applicantId);

            // Assert
            result.ShouldBe(expected);
            await _applicantContactQueryService.Received(1).GetByApplicantIdAsync(applicantId);
        }

        [Fact]
        public async Task UpdateAsync_ShouldMapInputAndUseApplicantEntityType()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var input = new UpdateApplicantContactDto
            {
                Name = "Updated",
                Title = "CFO",
                Email = "updated@example.com",
                MobilePhoneNumber = "555-3333",
                WorkPhoneNumber = "555-4444",
                WorkPhoneExtension = "99",
                Role = "Financial",
                IsPrimary = false
            };

            var returnedContact = new Contact
            {
                Name = input.Name,
                Title = input.Title,
                Email = input.Email,
                MobilePhoneNumber = input.MobilePhoneNumber,
                WorkPhoneNumber = input.WorkPhoneNumber,
                WorkPhoneExtension = input.WorkPhoneExtension
            };
            var returnedLink = new ContactLink
            {
                Role = input.Role,
                IsPrimary = input.IsPrimary
            };
            _contactManager.UpdateAsync(
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<ContactInput>(),
                    Arg.Any<string?>(),
                    Arg.Any<bool>())
                .Returns((returnedContact, returnedLink));

            // Act
            var result = await _service.UpdateAsync(applicantId, contactId, input);

            // Assert
            await _contactManager.Received(1).UpdateAsync(
                "Applicant",
                applicantId,
                contactId,
                Arg.Is<ContactInput>(ci =>
                    ci.Name == input.Name
                    && ci.Title == input.Title
                    && ci.Email == input.Email
                    && ci.HomePhoneNumber == null
                    && ci.MobilePhoneNumber == input.MobilePhoneNumber
                    && ci.WorkPhoneNumber == input.WorkPhoneNumber
                    && ci.WorkPhoneExtension == input.WorkPhoneExtension),
                input.Role,
                input.IsPrimary);

            result.Name.ShouldBe(input.Name);
            result.Role.ShouldBe(input.Role);
            result.IsPrimary.ShouldBe(input.IsPrimary);
        }

        [Fact]
        public async Task UpdateAsync_WithNullInput_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _service.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), null!));
        }

        [Fact]
        public async Task SetPrimaryAsync_ShouldDelegateWithApplicantEntityType()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            // Act
            await _service.SetPrimaryAsync(applicantId, contactId);

            // Assert
            await _contactManager.Received(1).SetPrimaryAsync("Applicant", applicantId, contactId);
        }
    }
}

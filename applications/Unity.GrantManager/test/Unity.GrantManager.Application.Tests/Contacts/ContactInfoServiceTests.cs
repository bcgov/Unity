using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.TestHelpers;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Unity.GrantManager.Contacts
{
    public class ApplicantProfileContactServiceTests
    {
        private readonly IContactRepository _contactRepository;
        private readonly IContactLinkRepository _contactLinkRepository;
        private readonly IRepository<ApplicationFormSubmission, Guid> _submissionRepository;
        private readonly IRepository<ApplicationContact, Guid> _applicationContactRepository;
        private readonly ApplicantProfileContactService _service;

        public ApplicantProfileContactServiceTests()
        {
            _contactRepository = Substitute.For<IContactRepository>();
            _contactLinkRepository = Substitute.For<IContactLinkRepository>();
            _submissionRepository = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            _applicationContactRepository = Substitute.For<IRepository<ApplicationContact, Guid>>();

            _service = new ApplicantProfileContactService(
                _contactRepository,
                _contactLinkRepository,
                _submissionRepository,
                _applicationContactRepository);
        }

        private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
        {
            EntityHelper.TrySetId(entity, () => id);
            return entity;
        }

        [Fact]
        public async Task GetProfileContactsAsync_WithMatchingLinks_ShouldReturnContacts()
        {
            // Arrange
            var profileId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var contacts = new[]
            {
                WithId(new Contact
                {
                    Name = "John Doe",
                    Title = "Manager",
                    Email = "john@example.com",
                    HomePhoneNumber = "111-1111",
                    MobilePhoneNumber = "222-2222",
                    WorkPhoneNumber = "333-3333",
                    WorkPhoneExtension = "101"
                }, contactId)
            }.AsAsyncQueryable();

            var contactLinks = new[]
            {
                new ContactLink
                {
                    ContactId = contactId,
                    RelatedEntityType = "ApplicantProfile",
                    RelatedEntityId = profileId,
                    Role = "Primary Contact",
                    IsPrimary = true,
                    IsActive = true
                }
            }.AsAsyncQueryable();

            _contactRepository.GetQueryableAsync().Returns(contacts);
            _contactLinkRepository.GetQueryableAsync().Returns(contactLinks);

            // Act
            var result = await _service.GetProfileContactsAsync(profileId);

            // Assert
            result.Count.ShouldBe(1);
            var contact = result[0];
            contact.ContactId.ShouldBe(contactId);
            contact.Name.ShouldBe("John Doe");
            contact.Title.ShouldBe("Manager");
            contact.Email.ShouldBe("john@example.com");
            contact.HomePhoneNumber.ShouldBe("111-1111");
            contact.MobilePhoneNumber.ShouldBe("222-2222");
            contact.WorkPhoneNumber.ShouldBe("333-3333");
            contact.WorkPhoneExtension.ShouldBe("101");
            contact.Role.ShouldBe("Primary Contact");
            contact.IsPrimary.ShouldBeTrue();
            contact.IsEditable.ShouldBeTrue();
            contact.ApplicationId.ShouldBeNull();
        }

        [Fact]
        public async Task GetProfileContactsAsync_WithNoLinks_ShouldReturnEmpty()
        {
            // Arrange
            _contactRepository.GetQueryableAsync().Returns(Array.Empty<Contact>().AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(Array.Empty<ContactLink>().AsAsyncQueryable());

            // Act
            var result = await _service.GetProfileContactsAsync(Guid.NewGuid());

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetApplicationContactsBySubjectAsync_WithMatchingSubmission_ShouldReturnContacts()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var appContactId = Guid.NewGuid();

            var submissions = new[]
            {
                new ApplicationFormSubmission
                {
                    OidcSub = "TESTUSER",
                    ApplicationId = applicationId,
                    ApplicantId = Guid.NewGuid(),
                    ApplicationFormId = Guid.NewGuid()
                }
            }.AsAsyncQueryable();

            var applicationContacts = new[]
            {
                WithId(new ApplicationContact
                {
                    ApplicationId = applicationId,
                    ContactFullName = "Jane Smith",
                    ContactTitle = "Director",
                    ContactEmail = "jane@example.com",
                    ContactMobilePhone = "444-4444",
                    ContactWorkPhone = "555-5555",
                    ContactType = "Signing Authority"
                }, appContactId)
            }.AsAsyncQueryable();

            _submissionRepository.GetQueryableAsync().Returns(submissions);
            _applicationContactRepository.GetQueryableAsync().Returns(applicationContacts);

            // Act
            var result = await _service.GetApplicationContactsBySubjectAsync("testuser@idir");

            // Assert
            result.Count.ShouldBe(1);
            var contact = result[0];
            contact.ContactId.ShouldBe(appContactId);
            contact.Name.ShouldBe("Jane Smith");
            contact.Title.ShouldBe("Director");
            contact.Email.ShouldBe("jane@example.com");
            contact.MobilePhoneNumber.ShouldBe("444-4444");
            contact.WorkPhoneNumber.ShouldBe("555-5555");
            contact.ContactType.ShouldBe("Signing Authority");
            contact.IsPrimary.ShouldBeFalse();
            contact.IsEditable.ShouldBeFalse();
            contact.ApplicationId.ShouldBe(applicationId);
        }

        [Fact]
        public async Task GetApplicationContactsBySubjectAsync_ShouldMatchCaseInsensitively()
        {
            // Arrange
            var applicationId = Guid.NewGuid();

            var submissions = new[]
            {
                new ApplicationFormSubmission
                {
                    OidcSub = "TESTUSER",
                    ApplicationId = applicationId,
                    ApplicantId = Guid.NewGuid(),
                    ApplicationFormId = Guid.NewGuid()
                }
            }.AsAsyncQueryable();

            var applicationContacts = new[]
            {
                WithId(new ApplicationContact
                {
                    ApplicationId = applicationId,
                    ContactFullName = "Case Test"
                }, Guid.NewGuid())
            }.AsAsyncQueryable();

            _submissionRepository.GetQueryableAsync().Returns(submissions);
            _applicationContactRepository.GetQueryableAsync().Returns(applicationContacts);

            // Act
            var result = await _service.GetApplicationContactsBySubjectAsync("testuser@IDIR");

            // Assert
            result.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetApplicationContactsBySubjectAsync_ShouldStripDomainFromSubject()
        {
            // Arrange
            var applicationId = Guid.NewGuid();

            var submissions = new[]
            {
                new ApplicationFormSubmission
                {
                    OidcSub = "MYUSER",
                    ApplicationId = applicationId,
                    ApplicantId = Guid.NewGuid(),
                    ApplicationFormId = Guid.NewGuid()
                }
            }.AsAsyncQueryable();

            var applicationContacts = new[]
            {
                WithId(new ApplicationContact
                {
                    ApplicationId = applicationId,
                    ContactFullName = "Domain Strip Test"
                }, Guid.NewGuid())
            }.AsAsyncQueryable();

            _submissionRepository.GetQueryableAsync().Returns(submissions);
            _applicationContactRepository.GetQueryableAsync().Returns(applicationContacts);

            // Act
            var result = await _service.GetApplicationContactsBySubjectAsync("myuser@differentdomain");

            // Assert
            result.Count.ShouldBe(1);
            result[0].Name.ShouldBe("Domain Strip Test");
        }

        [Fact]
        public async Task GetApplicationContactsBySubjectAsync_WithSubjectWithoutAtSign_ShouldStillMatch()
        {
            // Arrange
            var applicationId = Guid.NewGuid();

            var submissions = new[]
            {
                new ApplicationFormSubmission
                {
                    OidcSub = "PLAINUSER",
                    ApplicationId = applicationId,
                    ApplicantId = Guid.NewGuid(),
                    ApplicationFormId = Guid.NewGuid()
                }
            }.AsAsyncQueryable();

            var applicationContacts = new[]
            {
                WithId(new ApplicationContact
                {
                    ApplicationId = applicationId,
                    ContactFullName = "Plain User Contact"
                }, Guid.NewGuid())
            }.AsAsyncQueryable();

            _submissionRepository.GetQueryableAsync().Returns(submissions);
            _applicationContactRepository.GetQueryableAsync().Returns(applicationContacts);

            // Act
            var result = await _service.GetApplicationContactsBySubjectAsync("plainuser");

            // Assert
            result.Count.ShouldBe(1);
        }

        [Fact]
        public async Task GetApplicationContactsBySubjectAsync_WithNonMatchingSubject_ShouldReturnEmpty()
        {
            // Arrange
            var applicationId = Guid.NewGuid();

            var submissions = new[]
            {
                new ApplicationFormSubmission
                {
                    OidcSub = "OTHERUSER",
                    ApplicationId = applicationId,
                    ApplicantId = Guid.NewGuid(),
                    ApplicationFormId = Guid.NewGuid()
                }
            }.AsAsyncQueryable();

            var applicationContacts = new[]
            {
                WithId(new ApplicationContact
                {
                    ApplicationId = applicationId,
                    ContactFullName = "Should Not Match"
                }, Guid.NewGuid())
            }.AsAsyncQueryable();

            _submissionRepository.GetQueryableAsync().Returns(submissions);
            _applicationContactRepository.GetQueryableAsync().Returns(applicationContacts);

            // Act
            var result = await _service.GetApplicationContactsBySubjectAsync("differentuser@idir");

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetApplicationContactsBySubjectAsync_WithNoSubmissions_ShouldReturnEmpty()
        {
            // Arrange
            _submissionRepository.GetQueryableAsync()
                .Returns(Array.Empty<ApplicationFormSubmission>().AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync()
                .Returns(Array.Empty<ApplicationContact>().AsAsyncQueryable());

            // Act
            var result = await _service.GetApplicationContactsBySubjectAsync("testuser@idir");

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetApplicationContactsBySubjectAsync_WithMultipleSubmissions_ShouldReturnAllContacts()
        {
            // Arrange
            var appId1 = Guid.NewGuid();
            var appId2 = Guid.NewGuid();

            var submissions = new[]
            {
                new ApplicationFormSubmission
                {
                    OidcSub = "TESTUSER",
                    ApplicationId = appId1,
                    ApplicantId = Guid.NewGuid(),
                    ApplicationFormId = Guid.NewGuid()
                },
                new ApplicationFormSubmission
                {
                    OidcSub = "TESTUSER",
                    ApplicationId = appId2,
                    ApplicantId = Guid.NewGuid(),
                    ApplicationFormId = Guid.NewGuid()
                }
            }.AsAsyncQueryable();

            var applicationContacts = new[]
            {
                WithId(new ApplicationContact
                {
                    ApplicationId = appId1,
                    ContactFullName = "Contact App 1"
                }, Guid.NewGuid()),
                WithId(new ApplicationContact
                {
                    ApplicationId = appId2,
                    ContactFullName = "Contact App 2"
                }, Guid.NewGuid())
            }.AsAsyncQueryable();

            _submissionRepository.GetQueryableAsync().Returns(submissions);
            _applicationContactRepository.GetQueryableAsync().Returns(applicationContacts);

            // Act
            var result = await _service.GetApplicationContactsBySubjectAsync("testuser@idir");

            // Assert
            result.Count.ShouldBe(2);
            result.ShouldAllBe(c => !c.IsEditable);
            result.ShouldAllBe(c => !c.IsPrimary);
        }
    }
}

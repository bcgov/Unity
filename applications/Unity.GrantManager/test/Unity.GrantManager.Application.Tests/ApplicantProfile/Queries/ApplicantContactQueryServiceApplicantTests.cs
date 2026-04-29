using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.TestHelpers;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Unity.GrantManager.ApplicantProfile
{
    public class ApplicantContactQueryServiceApplicantTests
    {
        private readonly IContactRepository _contactRepository;
        private readonly IContactLinkRepository _contactLinkRepository;
        private readonly IRepository<ApplicationFormSubmission, Guid> _submissionRepository;
        private readonly IRepository<ApplicationContact, Guid> _applicationContactRepository;
        private readonly IRepository<ApplicantAgent, Guid> _applicantAgentRepository;
        private readonly IRepository<Application, Guid> _applicationRepository;
        private readonly ApplicantContactQueryService _service;

        public ApplicantContactQueryServiceApplicantTests()
        {
            _contactRepository = Substitute.For<IContactRepository>();
            _contactLinkRepository = Substitute.For<IContactLinkRepository>();
            _submissionRepository = Substitute.For<IRepository<ApplicationFormSubmission, Guid>>();
            _applicationContactRepository = Substitute.For<IRepository<ApplicationContact, Guid>>();
            _applicantAgentRepository = Substitute.For<IRepository<ApplicantAgent, Guid>>();
            _applicationRepository = Substitute.For<IRepository<Application, Guid>>();

            _service = new ApplicantContactQueryService(
                _contactRepository,
                _contactLinkRepository,
                _submissionRepository,
                _applicationContactRepository,
                _applicantAgentRepository,
                _applicationRepository);
        }

        private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
        {
            EntityHelper.TrySetId(entity, () => id);
            return entity;
        }

        private void SetupEmptyRepositories()
        {
            _contactRepository.GetQueryableAsync().Returns(Array.Empty<Contact>().AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(Array.Empty<ContactLink>().AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync().Returns(Array.Empty<ApplicationContact>().AsAsyncQueryable());
            _applicantAgentRepository.GetQueryableAsync().Returns(Array.Empty<ApplicantAgent>().AsAsyncQueryable());
            _applicationRepository.GetQueryableAsync().Returns(Array.Empty<Application>().AsAsyncQueryable());
        }

        [Fact]
        public async Task GetByApplicantIdAsync_WithEmptyGuid_ShouldReturnEmptyDto()
        {
            // Arrange
            SetupEmptyRepositories();

            // Act
            var result = await _service.GetByApplicantIdAsync(Guid.Empty);

            // Assert
            result.ShouldNotBeNull();
            result.Contacts.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByApplicantIdAsync_WithNoData_ShouldReturnEmptyDto()
        {
            // Arrange
            SetupEmptyRepositories();

            // Act
            var result = await _service.GetByApplicantIdAsync(Guid.NewGuid());

            // Assert
            result.Contacts.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByApplicantIdAsync_ShouldAggregateAllThreeSources()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var contact = WithId(new Contact { Name = "Applicant Linked", CreationTime = new DateTime(2024, 1, 1) }, contactId);
            var link = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "Applicant",
                RelatedEntityId = applicantId,
                IsActive = true,
                IsPrimary = false,
                Role = "General"
            };
            var application = WithId(new Application { ApplicantId = applicantId, ReferenceNo = "APP-001" }, applicationId);
            var appContact = WithId(new ApplicationContact
            {
                ApplicationId = applicationId,
                ContactType = "Primary",
                ContactFullName = "App Contact",
                CreationTime = new DateTime(2024, 2, 1)
            }, Guid.NewGuid());
            var agent = WithId(new ApplicantAgent
            {
                ApplicantId = applicantId,
                ApplicationId = applicationId,
                Name = "Agent",
                RoleForApplicant = "Agent",
                CreationTime = new DateTime(2024, 3, 1)
            }, Guid.NewGuid());

            _contactRepository.GetQueryableAsync().Returns(new[] { contact }.AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(new[] { link }.AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync().Returns(new[] { appContact }.AsAsyncQueryable());
            _applicantAgentRepository.GetQueryableAsync().Returns(new[] { agent }.AsAsyncQueryable());
            _applicationRepository.GetQueryableAsync().Returns(new[] { application }.AsAsyncQueryable());

            // Act
            var result = await _service.GetByApplicantIdAsync(applicantId);

            // Assert
            result.Contacts.Count.ShouldBe(3);
            result.Contacts.ShouldContain(c => c.ContactType == "Applicant" && c.IsEditable);
            result.Contacts.ShouldContain(c => c.ContactType == "Application" && !c.IsEditable && c.ReferenceNo == "APP-001");
            result.Contacts.ShouldContain(c => c.ContactType == "ApplicantAgent" && !c.IsEditable && c.ReferenceNo == "APP-001");
        }

        [Fact]
        public async Task GetByApplicantIdAsync_ShouldExcludeInactiveLinks()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var contact = WithId(new Contact { Name = "Inactive" }, contactId);
            var link = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "Applicant",
                RelatedEntityId = applicantId,
                IsActive = false
            };

            _contactRepository.GetQueryableAsync().Returns(new[] { contact }.AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(new[] { link }.AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync().Returns(Array.Empty<ApplicationContact>().AsAsyncQueryable());
            _applicantAgentRepository.GetQueryableAsync().Returns(Array.Empty<ApplicantAgent>().AsAsyncQueryable());
            _applicationRepository.GetQueryableAsync().Returns(Array.Empty<Application>().AsAsyncQueryable());

            // Act
            var result = await _service.GetByApplicantIdAsync(applicantId);

            // Assert
            result.Contacts.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByApplicantIdAsync_ShouldExcludeOtherApplicants()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var otherApplicantId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var contact = WithId(new Contact { Name = "Other" }, contactId);
            var link = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "Applicant",
                RelatedEntityId = otherApplicantId,
                IsActive = true
            };

            _contactRepository.GetQueryableAsync().Returns(new[] { contact }.AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(new[] { link }.AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync().Returns(Array.Empty<ApplicationContact>().AsAsyncQueryable());
            _applicantAgentRepository.GetQueryableAsync().Returns(Array.Empty<ApplicantAgent>().AsAsyncQueryable());
            _applicationRepository.GetQueryableAsync().Returns(Array.Empty<Application>().AsAsyncQueryable());

            // Act
            var result = await _service.GetByApplicantIdAsync(applicantId);

            // Assert
            result.Contacts.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByApplicantIdAsync_NoExplicitPrimary_ShouldMarkLatestAsPrimary()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var oldContactId = Guid.NewGuid();
            var newContactId = Guid.NewGuid();

            var contacts = new[]
            {
                WithId(new Contact { Name = "Older", CreationTime = new DateTime(2024, 1, 1) }, oldContactId),
                WithId(new Contact { Name = "Newest", CreationTime = new DateTime(2024, 6, 15) }, newContactId)
            };
            var links = new[]
            {
                new ContactLink { ContactId = oldContactId, RelatedEntityType = "Applicant", RelatedEntityId = applicantId, IsActive = true, IsPrimary = false },
                new ContactLink { ContactId = newContactId, RelatedEntityType = "Applicant", RelatedEntityId = applicantId, IsActive = true, IsPrimary = false }
            };

            _contactRepository.GetQueryableAsync().Returns(contacts.AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(links.AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync().Returns(Array.Empty<ApplicationContact>().AsAsyncQueryable());
            _applicantAgentRepository.GetQueryableAsync().Returns(Array.Empty<ApplicantAgent>().AsAsyncQueryable());
            _applicationRepository.GetQueryableAsync().Returns(Array.Empty<Application>().AsAsyncQueryable());

            // Act
            var result = await _service.GetByApplicantIdAsync(applicantId);

            // Assert
            result.Contacts.Count(c => c.IsPrimary).ShouldBe(1);
            result.Contacts.First(c => c.IsPrimary).Name.ShouldBe("Newest");
        }

        [Fact]
        public async Task GetByApplicantIdAsync_ExplicitPrimary_ShouldNotChangePrimary()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var oldContactId = Guid.NewGuid();
            var newContactId = Guid.NewGuid();

            var contacts = new[]
            {
                WithId(new Contact { Name = "Older Primary", CreationTime = new DateTime(2024, 1, 1) }, oldContactId),
                WithId(new Contact { Name = "Newer Not Primary", CreationTime = new DateTime(2024, 6, 15) }, newContactId)
            };
            var links = new[]
            {
                new ContactLink { ContactId = oldContactId, RelatedEntityType = "Applicant", RelatedEntityId = applicantId, IsActive = true, IsPrimary = true },
                new ContactLink { ContactId = newContactId, RelatedEntityType = "Applicant", RelatedEntityId = applicantId, IsActive = true, IsPrimary = false }
            };

            _contactRepository.GetQueryableAsync().Returns(contacts.AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(links.AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync().Returns(Array.Empty<ApplicationContact>().AsAsyncQueryable());
            _applicantAgentRepository.GetQueryableAsync().Returns(Array.Empty<ApplicantAgent>().AsAsyncQueryable());
            _applicationRepository.GetQueryableAsync().Returns(Array.Empty<Application>().AsAsyncQueryable());

            // Act
            var result = await _service.GetByApplicantIdAsync(applicantId);

            // Assert
            result.Contacts.Count(c => c.IsPrimary).ShouldBe(1);
            result.Contacts.First(c => c.IsPrimary).Name.ShouldBe("Older Primary");
        }

        [Fact]
        public async Task GetByApplicantIdAsync_ApplicationContactsAndAgents_ShouldBeReadOnly()
        {
            // Arrange
            var applicantId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();

            var application = WithId(new Application { ApplicantId = applicantId, ReferenceNo = "APP-XYZ" }, applicationId);
            var appContact = WithId(new ApplicationContact
            {
                ApplicationId = applicationId,
                ContactType = "Financial",
                ContactFullName = "Finance Person"
            }, Guid.NewGuid());
            var agent = WithId(new ApplicantAgent
            {
                ApplicantId = applicantId,
                ApplicationId = applicationId,
                Name = "Agent Person",
                RoleForApplicant = "Executive"
            }, Guid.NewGuid());

            _contactRepository.GetQueryableAsync().Returns(Array.Empty<Contact>().AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(Array.Empty<ContactLink>().AsAsyncQueryable());
            _applicationContactRepository.GetQueryableAsync().Returns(new[] { appContact }.AsAsyncQueryable());
            _applicantAgentRepository.GetQueryableAsync().Returns(new[] { agent }.AsAsyncQueryable());
            _applicationRepository.GetQueryableAsync().Returns(new[] { application }.AsAsyncQueryable());

            // Act
            var result = await _service.GetByApplicantIdAsync(applicantId);

            // Assert
            result.Contacts.Count.ShouldBe(2);
            result.Contacts.All(c => !c.IsEditable).ShouldBeTrue();
            result.Contacts.All(c => c.ReferenceNo == "APP-XYZ").ShouldBeTrue();
        }
    }
}

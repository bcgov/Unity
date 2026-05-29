using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.TestHelpers;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.Contacts
{
    public class ContactAppServiceTests
    {
        private readonly IContactRepository _contactRepository;
        private readonly IContactLinkRepository _contactLinkRepository;
        private readonly IContactManager _contactManager;
        private readonly ContactAppService _service;

        public ContactAppServiceTests()
        {
            _contactRepository = Substitute.For<IContactRepository>();
            _contactLinkRepository = Substitute.For<IContactLinkRepository>();
            _contactManager = Substitute.For<IContactManager>();

            _service = new ContactAppService(
                _contactRepository,
                _contactLinkRepository,
                _contactManager);
        }

        private static T WithId<T>(T entity, Guid id) where T : Entity<Guid>
        {
            EntityHelper.TrySetId(entity, () => id);
            return entity;
        }

        #region GetContactsByEntityAsync

        [Fact]
        public async Task GetContactsByEntityAsync_WithMatchingLinks_ShouldReturnAllFields()
        {
            // Arrange
            var entityId = Guid.NewGuid();
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
                    RelatedEntityType = "TestEntity",
                    RelatedEntityId = entityId,
                    Role = "Primary Contact",
                    IsPrimary = true,
                    IsActive = true
                }
            }.AsAsyncQueryable();

            _contactRepository.GetQueryableAsync().Returns(contacts);
            _contactLinkRepository.GetQueryableAsync().Returns(contactLinks);

            // Act
            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

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
        }

        [Fact]
        public async Task GetContactsByEntityAsync_WithMultipleContacts_ShouldReturnAll()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId1 = Guid.NewGuid();
            var contactId2 = Guid.NewGuid();

            var contacts = new[]
            {
                WithId(new Contact { Name = "Contact One" }, contactId1),
                WithId(new Contact { Name = "Contact Two" }, contactId2)
            }.AsAsyncQueryable();

            var contactLinks = new[]
            {
                new ContactLink
                {
                    ContactId = contactId1,
                    RelatedEntityType = "TestEntity",
                    RelatedEntityId = entityId,
                    IsPrimary = true,
                    IsActive = true
                },
                new ContactLink
                {
                    ContactId = contactId2,
                    RelatedEntityType = "TestEntity",
                    RelatedEntityId = entityId,
                    IsPrimary = false,
                    IsActive = true
                }
            }.AsAsyncQueryable();

            _contactRepository.GetQueryableAsync().Returns(contacts);
            _contactLinkRepository.GetQueryableAsync().Returns(contactLinks);

            // Act
            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

            // Assert
            result.Count.ShouldBe(2);
            result.ShouldContain(c => c.Name == "Contact One" && c.IsPrimary);
            result.ShouldContain(c => c.Name == "Contact Two" && !c.IsPrimary);
        }

        [Fact]
        public async Task GetContactsByEntityAsync_ShouldExcludeInactiveLinks()
        {
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var contacts = new[]
            {
                WithId(new Contact { Name = "Inactive Contact" }, contactId)
            }.AsAsyncQueryable();

            var contactLinks = new[]
            {
                new ContactLink
                {
                    ContactId = contactId,
                    RelatedEntityType = "TestEntity",
                    RelatedEntityId = entityId,
                    IsActive = false
                }
            }.AsAsyncQueryable();

            _contactRepository.GetQueryableAsync().Returns(contacts);
            _contactLinkRepository.GetQueryableAsync().Returns(contactLinks);

            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetContactsByEntityAsync_ShouldExcludeDifferentEntityType()
        {
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var contacts = new[]
            {
                WithId(new Contact { Name = "Wrong Type" }, contactId)
            }.AsAsyncQueryable();

            var contactLinks = new[]
            {
                new ContactLink
                {
                    ContactId = contactId,
                    RelatedEntityType = "OtherType",
                    RelatedEntityId = entityId,
                    IsActive = true
                }
            }.AsAsyncQueryable();

            _contactRepository.GetQueryableAsync().Returns(contacts);
            _contactLinkRepository.GetQueryableAsync().Returns(contactLinks);

            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetContactsByEntityAsync_ShouldExcludeDifferentEntityId()
        {
            var entityId = Guid.NewGuid();
            var otherEntityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var contacts = new[]
            {
                WithId(new Contact { Name = "Other Entity" }, contactId)
            }.AsAsyncQueryable();

            var contactLinks = new[]
            {
                new ContactLink
                {
                    ContactId = contactId,
                    RelatedEntityType = "TestEntity",
                    RelatedEntityId = otherEntityId,
                    IsActive = true
                }
            }.AsAsyncQueryable();

            _contactRepository.GetQueryableAsync().Returns(contacts);
            _contactLinkRepository.GetQueryableAsync().Returns(contactLinks);

            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetContactsByEntityAsync_WithNoLinks_ShouldReturnEmpty()
        {
            _contactRepository.GetQueryableAsync().Returns(Array.Empty<Contact>().AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(Array.Empty<ContactLink>().AsAsyncQueryable());

            var result = await _service.GetContactsByEntityAsync("TestEntity", Guid.NewGuid());

            result.ShouldBeEmpty();
        }

        #endregion

        #region CreateContactAsync

        [Fact]
        public async Task CreateContactAsync_ShouldDelegateToContactManagerAndMapDto()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var input = new CreateContactLinkDto
            {
                Name = "New Contact",
                Title = "Analyst",
                Email = "new@example.com",
                HomePhoneNumber = "111-1111",
                MobilePhoneNumber = "222-2222",
                WorkPhoneNumber = "333-3333",
                WorkPhoneExtension = "101",
                Role = "Reviewer",
                IsPrimary = true,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId
            };

            var contact = WithId(new Contact
            {
                Name = input.Name,
                Title = input.Title,
                Email = input.Email,
                HomePhoneNumber = input.HomePhoneNumber,
                MobilePhoneNumber = input.MobilePhoneNumber,
                WorkPhoneNumber = input.WorkPhoneNumber,
                WorkPhoneExtension = input.WorkPhoneExtension
            }, contactId);
            var link = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = input.RelatedEntityType,
                RelatedEntityId = entityId,
                Role = input.Role,
                IsPrimary = input.IsPrimary,
                IsActive = true
            };

            _contactManager.CreateAsync(
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<ContactInput>(),
                    Arg.Any<string?>(),
                    Arg.Any<bool>())
                .Returns((contact, link));

            // Act
            var result = await _service.CreateContactAsync(input);

            // Assert
            await _contactManager.Received(1).CreateAsync(
                input.RelatedEntityType,
                input.RelatedEntityId,
                Arg.Is<ContactInput>(ci =>
                    ci.Name == input.Name
                    && ci.Title == input.Title
                    && ci.Email == input.Email
                    && ci.HomePhoneNumber == input.HomePhoneNumber
                    && ci.MobilePhoneNumber == input.MobilePhoneNumber
                    && ci.WorkPhoneNumber == input.WorkPhoneNumber
                    && ci.WorkPhoneExtension == input.WorkPhoneExtension),
                input.Role,
                input.IsPrimary);

            result.ContactId.ShouldBe(contactId);
            result.Name.ShouldBe(input.Name);
            result.Title.ShouldBe(input.Title);
            result.Email.ShouldBe(input.Email);
            result.HomePhoneNumber.ShouldBe(input.HomePhoneNumber);
            result.MobilePhoneNumber.ShouldBe(input.MobilePhoneNumber);
            result.WorkPhoneNumber.ShouldBe(input.WorkPhoneNumber);
            result.WorkPhoneExtension.ShouldBe(input.WorkPhoneExtension);
            result.Role.ShouldBe(input.Role);
            result.IsPrimary.ShouldBe(input.IsPrimary);
        }

        [Fact]
        public async Task CreateContactAsync_WithNullInput_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _service.CreateContactAsync(null!));
        }

        #endregion

        #region UpdateContactAsync

        [Fact]
        public async Task UpdateContactAsync_ShouldDelegateToContactManagerAndMapDto()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var input = new UpdateContactDto
            {
                Name = "Updated",
                Title = "CFO",
                Email = "updated@example.com",
                HomePhoneNumber = "111-0000",
                MobilePhoneNumber = "555-3333",
                WorkPhoneNumber = "555-4444",
                WorkPhoneExtension = "99",
                Role = "Financial",
                IsPrimary = false
            };

            var contact = WithId(new Contact
            {
                Name = input.Name,
                Title = input.Title,
                Email = input.Email,
                HomePhoneNumber = input.HomePhoneNumber,
                MobilePhoneNumber = input.MobilePhoneNumber,
                WorkPhoneNumber = input.WorkPhoneNumber,
                WorkPhoneExtension = input.WorkPhoneExtension
            }, contactId);
            var link = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                Role = input.Role,
                IsPrimary = input.IsPrimary,
                IsActive = true
            };

            _contactManager.UpdateAsync(
                    Arg.Any<string>(),
                    Arg.Any<Guid>(),
                    Arg.Any<Guid>(),
                    Arg.Any<ContactInput>(),
                    Arg.Any<string?>(),
                    Arg.Any<bool>())
                .Returns((contact, link));

            // Act
            var result = await _service.UpdateContactAsync("TestEntity", entityId, contactId, input);

            // Assert
            await _contactManager.Received(1).UpdateAsync(
                "TestEntity",
                entityId,
                contactId,
                Arg.Is<ContactInput>(ci =>
                    ci.Name == input.Name
                    && ci.Title == input.Title
                    && ci.Email == input.Email
                    && ci.HomePhoneNumber == input.HomePhoneNumber
                    && ci.MobilePhoneNumber == input.MobilePhoneNumber
                    && ci.WorkPhoneNumber == input.WorkPhoneNumber
                    && ci.WorkPhoneExtension == input.WorkPhoneExtension),
                input.Role,
                input.IsPrimary);

            result.ContactId.ShouldBe(contactId);
            result.Name.ShouldBe(input.Name);
            result.Role.ShouldBe(input.Role);
            result.IsPrimary.ShouldBe(input.IsPrimary);
        }

        [Fact]
        public async Task UpdateContactAsync_WithNullInput_ShouldThrow()
        {
            await Should.ThrowAsync<ArgumentNullException>(
                () => _service.UpdateContactAsync("TestEntity", Guid.NewGuid(), Guid.NewGuid(), null!));
        }

        #endregion

        #region SetPrimaryContactAsync

        [Fact]
        public async Task SetPrimaryContactAsync_ShouldDelegateToContactManager()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            // Act
            await _service.SetPrimaryContactAsync("TestEntity", entityId, contactId);

            // Assert
            await _contactManager.Received(1).SetPrimaryAsync("TestEntity", entityId, contactId);
        }

        #endregion
    }
}

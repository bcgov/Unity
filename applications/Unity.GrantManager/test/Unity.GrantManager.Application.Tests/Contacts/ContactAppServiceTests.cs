using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.TestHelpers;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.Contacts
{
    public class ContactAppServiceTests
    {
        private readonly IContactRepository _contactRepository;
        private readonly IContactLinkRepository _contactLinkRepository;
        private readonly ContactAppService _service;

        public ContactAppServiceTests()
        {
            _contactRepository = Substitute.For<IContactRepository>();
            _contactLinkRepository = Substitute.For<IContactLinkRepository>();

            _service = new ContactAppService(
                _contactRepository,
                _contactLinkRepository);
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
            // Arrange
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

            // Act
            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetContactsByEntityAsync_ShouldExcludeDifferentEntityType()
        {
            // Arrange
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

            // Act
            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetContactsByEntityAsync_ShouldExcludeDifferentEntityId()
        {
            // Arrange
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

            // Act
            var result = await _service.GetContactsByEntityAsync("TestEntity", entityId);

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetContactsByEntityAsync_WithNoLinks_ShouldReturnEmpty()
        {
            // Arrange
            _contactRepository.GetQueryableAsync().Returns(Array.Empty<Contact>().AsAsyncQueryable());
            _contactLinkRepository.GetQueryableAsync().Returns(Array.Empty<ContactLink>().AsAsyncQueryable());

            // Act
            var result = await _service.GetContactsByEntityAsync("TestEntity", Guid.NewGuid());

            // Assert
            result.ShouldBeEmpty();
        }

        #endregion

        #region CreateContactAsync

        [Fact]
        public async Task CreateContactAsync_ShouldCreateContactAndLink()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            _contactRepository.InsertAsync(Arg.Any<Contact>(), true, Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var c = ci.Arg<Contact>();
                    EntityHelper.TrySetId(c, () => contactId);
                    return c;
                });

            _contactLinkRepository.GetQueryableAsync()
                .Returns(Array.Empty<ContactLink>().AsAsyncQueryable());

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
                IsPrimary = false,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId
            };

            // Act
            var result = await _service.CreateContactAsync(input);

            // Assert
            result.ContactId.ShouldBe(contactId);
            result.Name.ShouldBe("New Contact");
            result.Title.ShouldBe("Analyst");
            result.Email.ShouldBe("new@example.com");
            result.HomePhoneNumber.ShouldBe("111-1111");
            result.MobilePhoneNumber.ShouldBe("222-2222");
            result.WorkPhoneNumber.ShouldBe("333-3333");
            result.WorkPhoneExtension.ShouldBe("101");
            result.Role.ShouldBe("Reviewer");
            result.IsPrimary.ShouldBeFalse();

            await _contactRepository.Received(1).InsertAsync(
                Arg.Is<Contact>(c =>
                    c.Name == "New Contact"
                    && c.Title == "Analyst"
                    && c.Email == "new@example.com"
                    && c.HomePhoneNumber == "111-1111"
                    && c.MobilePhoneNumber == "222-2222"
                    && c.WorkPhoneNumber == "333-3333"
                    && c.WorkPhoneExtension == "101"),
                true,
                Arg.Any<CancellationToken>());

            await _contactLinkRepository.Received(1).InsertAsync(
                Arg.Is<ContactLink>(l =>
                    l.ContactId == contactId
                    && l.RelatedEntityType == "TestEntity"
                    && l.RelatedEntityId == entityId
                    && l.Role == "Reviewer"
                    && !l.IsPrimary
                    && l.IsActive),
                true,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateContactAsync_NonPrimary_ShouldNotClearExistingPrimary()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            _contactRepository.InsertAsync(Arg.Any<Contact>(), true, Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var c = ci.Arg<Contact>();
                    EntityHelper.TrySetId(c, () => contactId);
                    return c;
                });

            var input = new CreateContactLinkDto
            {
                Name = "Non-Primary Contact",
                IsPrimary = false,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId
            };

            // Act
            await _service.CreateContactAsync(input);

            // Assert — GetQueryableAsync should not be called (ClearPrimaryAsync not invoked)
            await _contactLinkRepository.DidNotReceive().GetQueryableAsync();
        }

        [Fact]
        public async Task CreateContactAsync_WhenPrimary_ShouldClearExistingPrimary()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var entityId = Guid.NewGuid();
            var existingLinkId = Guid.NewGuid();

            var existingLink = new ContactLink
            {
                ContactId = Guid.NewGuid(),
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = true,
                IsActive = true
            };
            EntityHelper.TrySetId(existingLink, () => existingLinkId);

            _contactLinkRepository.GetQueryableAsync()
                .Returns(
                    new[] { existingLink }.AsAsyncQueryable(),
                    Array.Empty<ContactLink>().AsAsyncQueryable());

            _contactRepository.InsertAsync(Arg.Any<Contact>(), true, Arg.Any<CancellationToken>())
                .Returns(ci =>
                {
                    var c = ci.Arg<Contact>();
                    EntityHelper.TrySetId(c, () => contactId);
                    return c;
                });

            var input = new CreateContactLinkDto
            {
                Name = "Primary Contact",
                IsPrimary = true,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId
            };

            // Act
            var result = await _service.CreateContactAsync(input);

            // Assert
            result.IsPrimary.ShouldBeTrue();
            await _contactLinkRepository.Received(1).UpdateAsync(
                Arg.Is<ContactLink>(l => l.Id == existingLinkId && !l.IsPrimary),
                true,
                Arg.Any<CancellationToken>());
        }

        #endregion

        #region SetPrimaryContactAsync

        [Fact]
        public async Task SetPrimaryContactAsync_ShouldClearExistingAndSetNew()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var existingPrimaryLinkId = Guid.NewGuid();
            var targetLinkId = Guid.NewGuid();

            var existingPrimaryLink = new ContactLink
            {
                ContactId = Guid.NewGuid(),
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = true,
                IsActive = true
            };
            EntityHelper.TrySetId(existingPrimaryLink, () => existingPrimaryLinkId);

            var targetLink = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = false,
                IsActive = true
            };
            EntityHelper.TrySetId(targetLink, () => targetLinkId);

            _contactLinkRepository.GetQueryableAsync()
                .Returns(
                    new[] { existingPrimaryLink }.AsAsyncQueryable(),
                    new[] { targetLink }.AsAsyncQueryable());

            // Act
            await _service.SetPrimaryContactAsync("TestEntity", entityId, contactId);

            // Assert
            await _contactLinkRepository.Received(1).UpdateAsync(
                Arg.Is<ContactLink>(l => l.Id == existingPrimaryLinkId && !l.IsPrimary),
                true,
                Arg.Any<CancellationToken>());
            await _contactLinkRepository.Received(1).UpdateAsync(
                Arg.Is<ContactLink>(l => l.Id == targetLinkId && l.IsPrimary),
                true,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetPrimaryContactAsync_WithNoExistingPrimary_ShouldSetNew()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var targetLinkId = Guid.NewGuid();

            var targetLink = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = false,
                IsActive = true
            };
            EntityHelper.TrySetId(targetLink, () => targetLinkId);

            _contactLinkRepository.GetQueryableAsync()
                .Returns(
                    Array.Empty<ContactLink>().AsAsyncQueryable(),
                    new[] { targetLink }.AsAsyncQueryable());

            // Act
            await _service.SetPrimaryContactAsync("TestEntity", entityId, contactId);

            // Assert — only the target link should be updated (set to primary)
            await _contactLinkRepository.Received(1).UpdateAsync(
                Arg.Is<ContactLink>(l => l.Id == targetLinkId && l.IsPrimary),
                true,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetPrimaryContactAsync_WithMultipleExistingPrimaries_ShouldClearAll()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var primaryLinkId1 = Guid.NewGuid();
            var primaryLinkId2 = Guid.NewGuid();
            var targetLinkId = Guid.NewGuid();

            var primaryLink1 = new ContactLink
            {
                ContactId = Guid.NewGuid(),
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = true,
                IsActive = true
            };
            EntityHelper.TrySetId(primaryLink1, () => primaryLinkId1);

            var primaryLink2 = new ContactLink
            {
                ContactId = Guid.NewGuid(),
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = true,
                IsActive = true
            };
            EntityHelper.TrySetId(primaryLink2, () => primaryLinkId2);

            var targetLink = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = false,
                IsActive = true
            };
            EntityHelper.TrySetId(targetLink, () => targetLinkId);

            _contactLinkRepository.GetQueryableAsync()
                .Returns(
                    new[] { primaryLink1, primaryLink2 }.AsAsyncQueryable(),
                    new[] { targetLink }.AsAsyncQueryable());

            // Act
            await _service.SetPrimaryContactAsync("TestEntity", entityId, contactId);

            // Assert — both existing primaries cleared
            await _contactLinkRepository.Received(1).UpdateAsync(
                Arg.Is<ContactLink>(l => l.Id == primaryLinkId1 && !l.IsPrimary),
                true,
                Arg.Any<CancellationToken>());
            await _contactLinkRepository.Received(1).UpdateAsync(
                Arg.Is<ContactLink>(l => l.Id == primaryLinkId2 && !l.IsPrimary),
                true,
                Arg.Any<CancellationToken>());
            // Target set as primary
            await _contactLinkRepository.Received(1).UpdateAsync(
                Arg.Is<ContactLink>(l => l.Id == targetLinkId && l.IsPrimary),
                true,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task SetPrimaryContactAsync_ShouldNotMatchInactiveLink()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var inactiveLink = new ContactLink
            {
                ContactId = contactId,
                RelatedEntityType = "TestEntity",
                RelatedEntityId = entityId,
                IsPrimary = false,
                IsActive = false
            };

            _contactLinkRepository.GetQueryableAsync()
                .Returns(
                    Array.Empty<ContactLink>().AsAsyncQueryable(),
                    new[] { inactiveLink }.AsAsyncQueryable());

            // Act & Assert
            await Should.ThrowAsync<BusinessException>(
                () => _service.SetPrimaryContactAsync("TestEntity", entityId, contactId));
        }

        [Fact]
        public async Task SetPrimaryContactAsync_WhenContactLinkNotFound_ShouldThrow()
        {
            // Arrange
            var entityId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            _contactLinkRepository.GetQueryableAsync()
                .Returns(
                    Array.Empty<ContactLink>().AsAsyncQueryable(),
                    Array.Empty<ContactLink>().AsAsyncQueryable());

            // Act & Assert
            var ex = await Should.ThrowAsync<BusinessException>(
                () => _service.SetPrimaryContactAsync("TestEntity", entityId, contactId));
            ex.Code.ShouldBe("Contacts:ContactLinkNotFound");
        }

        #endregion
    }
}

using Unity.GrantManager.Contacts;
using Xunit;

namespace Unity.GrantManager.Domain.Tests.Contacts
{
    public class ContactTypeHelperTests : GrantManagerDomainTestBase
    {
        [Fact]
        public void GetApplicantContactTypes_ReturnsAllContactTypes()
        {
            // Act
            var result = ContactTypeHelper.GetApplicantContactTypes();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result.Count);
        }

        [Fact]
        public void GetApplicantContactTypes_ReturnsContactTypesWithCorrectProperties()
        {
            // Act
            var result = ContactTypeHelper.GetApplicantContactTypes();

            // Assert
            Assert.All(result, contactType =>
            {
                Assert.NotNull(contactType.Value);
                Assert.NotEmpty(contactType.Value);
                Assert.NotNull(contactType.Display);
                Assert.NotEmpty(contactType.Display);
            });
        }

        [Fact]
        public void GetApplicantContactTypes_ContainsSigningAuthority()
        {
            // Act
            var result = ContactTypeHelper.GetApplicantContactTypes();

            // Assert
            var signingAuthority = result.Find(x => x.Value == "SIGNING_AUTHORITY");
            Assert.NotNull(signingAuthority);
            Assert.Equal("Signing Authority", signingAuthority.Display);
        }

        [Fact]
        public void GetApplicantContactTypes_ContainsContactPerson()
        {
            // Act
            var result = ContactTypeHelper.GetApplicantContactTypes();

            // Assert
            var contactPerson = result.Find(x => x.Value == "CONTACT_PERSON");
            Assert.NotNull(contactPerson);
            Assert.Equal("Contact Person", contactPerson.Display);
        }

        [Fact]
        public void GetApplicantContactTypes_ContainsAllExpectedValues()
        {
            // Act
            var result = ContactTypeHelper.GetApplicantContactTypes();

            // Assert
            Assert.Contains(result, x => x.Value == "SIGNING_AUTHORITY");
            Assert.Contains(result, x => x.Value == "CONTACT_PERSON");
            Assert.Contains(result, x => x.Value == "OFFICER");
            Assert.Contains(result, x => x.Value == "SUBMITTER");
            Assert.Contains(result, x => x.Value == "CONSULTANT");
            Assert.Contains(result, x => x.Value == "GRANT_WRITER");
        }

        [Fact]
        public void GetApplicantContactTypes_DisplayNamesMatchEnumDisplayAttributes()
        {
            // Act
            var result = ContactTypeHelper.GetApplicantContactTypes();

            // Assert
            var expectedDisplayNames = new[]
            {
                "Signing Authority",
                "Contact Person",
                "Officer",
                "Submitter",
                "Consultant",
                "Grant Writer"
            };

            Assert.Equal(expectedDisplayNames.Length, result.Count);
            foreach (var displayName in expectedDisplayNames)
            {
                Assert.Contains(result, x => x.Display == displayName);
            }
        }

        [Fact]
        public void ContactTypeDto_CanBeInstantiated()
        {
            // Act
            var dto = new ContactTypeHelper.ContactTypeDto
            {
                Value = "TEST_VALUE",
                Display = "Test Display"
            };

            // Assert
            Assert.Equal("TEST_VALUE", dto.Value);
            Assert.Equal("Test Display", dto.Display);
        }
    }
}

using Unity.GrantManager.Applications;
using Xunit;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.ApplicationForms
{
    public class ApplicationFormTests : GrantManagerDomainTestBase
    {
        /// <summary>
        /// Tests that the SetElectoralDistrictAddressType method correctly sets the address type.
        /// </summary>
        [Fact]
        public void GetDefaultElectoralDistrictAddressTypeReturnsExpected()
        {
            var result = ApplicationForm.GetDefaultElectoralDistrictAddressType();
            Assert.Equal(AddressType.PhysicalAddress, result);
        }

        /// <summary>
        /// Tests that the SetElectoralDistrictAddressType method correctly sets the address type.
        /// </summary>
        [Fact]
        public void GetAvailableElectoralDistrictAddressTypesReturnsExpected()
        {
            var result = ApplicationForm.GetAvailableElectoralDistrictAddressTypes();
            Assert.Equal(2, result.Count);
            Assert.Contains(
                result,
                x => x.AddressType == AddressType.PhysicalAddress
            );
            Assert.Contains(
                result,
                x => x.AddressType == AddressType.MailingAddress
            );
        }
    }
}


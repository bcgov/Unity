using Shouldly;
using System.Collections.Generic;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp;
using Xunit;

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

        [Fact]
        public void SetExternalLinks_ShouldThrow_WhenRenewalLinkPublishedWithoutUri()
        {
            var form = new ApplicationForm();

            var exception = Should.Throw<BusinessException>(() =>
                form.SetExternalLinks(
                    new ExternalLink { Uri = string.Empty, Published = true },
                    []));

            exception.Code.ShouldBe(GrantManagerDomainErrorCodes.RenewalLinkRequiredForVisibility);
        }

        [Fact]
        public void SetExternalLinks_ShouldThrow_WhenRelatedLinkPublishedWithoutUri()
        {
            var form = new ApplicationForm();

            var exception = Should.Throw<BusinessException>(() =>
                form.SetExternalLinks(
                    null,
                    [new ExternalLink { Uri = string.Empty, Published = true }]));

            exception.Code.ShouldBe(GrantManagerDomainErrorCodes.RelatedLinkInvalidUri);
        }

        [Fact]
        public void SetExternalLinks_ShouldThrow_WhenExceedingMaxRelatedLinks()
        {
            var form = new ApplicationForm();
            var relatedLinks = new List<ExternalLink>();
            for (var i = 0; i <= ApplicationForm.MaxRelatedExternalLinks; i++)
            {
                relatedLinks.Add(new ExternalLink { Uri = $"https://example.com/{i}" });
            }

            var exception = Should.Throw<BusinessException>(() =>
                form.SetExternalLinks(null, relatedLinks));

            exception.Code.ShouldBe(GrantManagerDomainErrorCodes.TooManyRelatedLinks);
        }

        [Fact]
        public void SetExternalLinks_ShouldAssignOrderAndTypes_WhenValid()
        {
            var form = new ApplicationForm();

            form.SetExternalLinks(
                new ExternalLink { Uri = "https://example.com/renew", Published = true },
                [
                    new ExternalLink { Uri = "https://example.com/one", Order = 2 },
                    new ExternalLink { Uri = "https://example.com/two", Order = 1}
                ]);

            form.ExternalLinks.Count.ShouldBe(3);
            form.ExternalLinks[0].ExternalLinkType.ShouldBe(ExternalLinkType.Renewal);
            form.ExternalLinks[0].Order.ShouldBe(-1);
            form.ExternalLinks[1].ExternalLinkType.ShouldBe(ExternalLinkType.Related);
            form.ExternalLinks[1].Order.ShouldBe(2);
            form.ExternalLinks[2].ExternalLinkType.ShouldBe(ExternalLinkType.Related);
            form.ExternalLinks[2].Order.ShouldBe(1);
        }
    }
}


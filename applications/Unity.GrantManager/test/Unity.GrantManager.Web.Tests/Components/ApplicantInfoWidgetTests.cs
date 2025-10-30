using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;
using Unity.GrantManager.Locality;
using Volo.Abp.DependencyInjection;
using Xunit;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Components
{
    public class ApplicantInfoWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public ApplicantInfoWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact]
        public async Task ContactInfoReturnsStatus()
        {
            // Arrange
            var contactInfo = new ContactInfoDto 
            { 
                Name = "John Doe", 
                Title = "Doctor", 
                Email = "john.doe@email.local", 
                Phone = "+12501234567", 
                Phone2 = "+12501234567" 
            };
            
            var signingAuthority = new SigningAuthorityDto
            {
                SigningAuthorityFullName = "Sam D", 
                SigningAuthorityTitle = "Director", 
                SigningAuthorityEmail = "sam.d@email.local", 
                SigningAuthorityBusinessPhone = "+12501234566", 
                SigningAuthorityCellPhone = "+12501234566"
            };
            
            var applicantInfoDto = new ApplicantInfoDto
            {
                ApplicantId = Guid.NewGuid(),
                ApplicationFormId = Guid.NewGuid(),
                ContactInfo = contactInfo,
                SigningAuthority = signingAuthority,
                ApplicantAddresses = new()
                {
                    new ApplicantAddressDto
                    { 
                        AddressType = AddressType.MailingAddress,
                        Street = "some street",
                        Unit = "some unit",
                        City = "some city",
                        Province = "some province",
                        Postal = "some postal"                        
                    },
                    new ApplicantAddressDto
                    {
                        AddressType = AddressType.PhysicalAddress,
                        Street = "some street",
                        Unit = "some unit",
                        City = "some city",
                        Province = "some province",
                        Postal = "some postal"
                    }
                }
            };

            var appService = Substitute.For<IApplicationApplicantAppService>();
            appService.GetApplicantInfoTabAsync(Arg.Any<Guid>()).Returns(applicantInfoDto);
            var sectorService = Substitute.For<ISectorService>();

            var applicationElectoralDistrictAppService = Substitute.For<IElectoralDistrictService>();
            var applicationFormAppService = Substitute.For<IApplicationFormAppService>();
            applicationFormAppService.GetAsync(Arg.Any<Guid>()).Returns(new ApplicationFormDto
            {
                ElectoralDistrictAddressType = ApplicationForm.GetDefaultElectoralDistrictAddressType()
            });

            var viewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ApplicantInfoViewComponent(appService, 
                sectorService,
                applicationElectoralDistrictAppService,
                applicationFormAppService)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            //Act
            var result = await viewComponent.InvokeAsync(Guid.NewGuid(), Guid.NewGuid()) as ViewViewComponentResult;
            ApplicantInfoViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ApplicantInfoViewModel;

            //Assert
            var expectedFullName = "John Doe";
            var expectedTitle = "Doctor";
            var expectedEmail = "john.doe@email.local";
            var expectedBusinessPhone = "+12501234567";
            var expectedCellPhone = "+12501234567";
            var expectedSigningAuthorityFullName = "Sam D";
            var expectedSigningAuthorityTitle = "Director";
            var expectedSigningAuthorityEmail = "sam.d@email.local";
            var expectedSigningAuthorityBusinessPhone = "+12501234566";
            var expectedSigningAuthorityCellPhone = "+12501234566";
            var expectedPhysicalAddressStreet = "some street";
            var expectedPhysicalAddressUnit = "some unit";
            var expectedPhysicalAddressCity = "some city";
            var expectedPhysicalAddressProvince = "some province";
            var expectedMailingAddressStreet = "some street";
            var expectedMailingAddressUnit = "some unit";
            var expectedMailingAddressCity = "some city";
            var expectedMailingAddressProvince = "some province";

            // Updated assertions to match the new structure with nested objects
            resultModel!.ContactInfo.Name.ShouldBe(expectedFullName);
            resultModel!.ContactInfo.Title.ShouldBe(expectedTitle);
            resultModel!.ContactInfo.Email.ShouldBe(expectedEmail);
            resultModel!.ContactInfo.Phone.ShouldBe(expectedBusinessPhone);
            resultModel!.ContactInfo.Phone2.ShouldBe(expectedCellPhone);
            resultModel!.SigningAuthority.SigningAuthorityFullName.ShouldBe(expectedSigningAuthorityFullName);
            resultModel!.SigningAuthority.SigningAuthorityTitle.ShouldBe(expectedSigningAuthorityTitle);
            resultModel!.SigningAuthority.SigningAuthorityEmail.ShouldBe(expectedSigningAuthorityEmail);
            resultModel!.SigningAuthority.SigningAuthorityBusinessPhone.ShouldBe(expectedSigningAuthorityBusinessPhone);
            resultModel!.SigningAuthority.SigningAuthorityCellPhone.ShouldBe(expectedSigningAuthorityCellPhone);

            resultModel!.PhysicalAddress.Street.ShouldBe(expectedPhysicalAddressStreet);
            resultModel!.PhysicalAddress.City.ShouldBe(expectedPhysicalAddressCity);
            resultModel!.PhysicalAddress.Unit.ShouldBe(expectedPhysicalAddressUnit);
            resultModel!.PhysicalAddress.Province.ShouldBe(expectedPhysicalAddressProvince);

            resultModel!.MailingAddress.Street.ShouldBe(expectedMailingAddressStreet);
            resultModel!.MailingAddress.City.ShouldBe(expectedMailingAddressCity);
            resultModel!.MailingAddress.Unit.ShouldBe(expectedMailingAddressUnit);
            resultModel!.MailingAddress.Province.ShouldBe(expectedMailingAddressProvince);
        }
    }
}

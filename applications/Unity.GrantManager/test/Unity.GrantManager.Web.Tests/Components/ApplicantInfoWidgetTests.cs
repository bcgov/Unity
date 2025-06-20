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
            var applicationDto = new ApplicationApplicantInfoDto()
            {
                ContactFullName = "John Doe",
                ContactTitle = "Doctor",
                ContactEmail = "john.doe@email.com",
                ContactBusinessPhone = "+12501234567",
                ContactCellPhone = "+12501234567",
                SigningAuthorityFullName = "Sam D",
                SigningAuthorityTitle = "Director",
                SigningAuthorityEmail = "sam.d@email.com",
                SigningAuthorityBusinessPhone = "+12501234566",
                SigningAuthorityCellPhone = "+12501234566",
                ApplicantAddresses =
                [
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
                    },
                ]
            };

            // Arrange
            var appService = Substitute.For<IApplicationApplicantAppService>();
            appService.GetByApplicationIdAsync(Arg.Any<Guid>()).Returns(applicationDto);
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
            var expectedEmail = "john.doe@email.com";
            var expectedBusinessPhone = "+12501234567";
            var expectedCellPhone = "+12501234567";
            var expectedSigningAuthorityFullName = "Sam D";
            var expectedSigningAuthorityTitle = "Director";
            var expectedSigningAuthorityEmail = "sam.d@email.com";
            var expectedSigningAuthorityBusinessPhone = "+12501234566";
            var expectedSigningAuthorityCellPhone = "+12501234566";
            var expectedPhysicalAddressStreet = "some street";
            var expectedPhysicalAddressUnit = "some unit";
            var expectedPhysicalAddressCity = "some city";
            var expectedPhysicalAddressProvince = "some province";
            var expectedPhysicalAddressPostalCode = "some postal";
            var expectedMailingAddressStreet = "some street";
            var expectedMailingAddressUnit = "some unit";
            var expectedMailingAddressCity = "some city";
            var expectedMailingAddressProvince = "some province";
            var expectedMailingAddressPostalCode = "some postal";

            //resultModel!.ContactFullName.ShouldBe(expectedFullName);
            //resultModel!.ContactTitle.ShouldBe(expectedTitle);
            //resultModel!.ContactEmail.ShouldBe(expectedEmail);
            //resultModel!.ContactBusinessPhone.ShouldBe(expectedBusinessPhone);
            //resultModel!.ContactCellPhone.ShouldBe(expectedCellPhone);
            //resultModel!.SigningAuthorityFullName.ShouldBe(expectedSigningAuthorityFullName);
            //resultModel!.SigningAuthorityTitle.ShouldBe(expectedSigningAuthorityTitle);
            //resultModel!.SigningAuthorityEmail.ShouldBe(expectedSigningAuthorityEmail);
            //resultModel!.SigningAuthorityBusinessPhone.ShouldBe(expectedSigningAuthorityBusinessPhone);
            //resultModel!.SigningAuthorityCellPhone.ShouldBe(expectedSigningAuthorityCellPhone);

            //resultModel!.PhysicalAddressStreet.ShouldBe(expectedPhysicalAddressStreet);
            //resultModel!.PhysicalAddressCity.ShouldBe(expectedPhysicalAddressCity);
            //resultModel!.PhysicalAddressUnit.ShouldBe(expectedPhysicalAddressUnit);
            //resultModel!.PhysicalAddressProvince.ShouldBe(expectedPhysicalAddressProvince);
            //resultModel!.PhysicalAddressPostalCode.ShouldBe(expectedPhysicalAddressPostalCode);
            //resultModel!.MailingAddressStreet.ShouldBe(expectedMailingAddressStreet);
            //resultModel!.MailingAddressCity.ShouldBe(expectedMailingAddressCity);
            //resultModel!.MailingAddressUnit.ShouldBe(expectedMailingAddressUnit);
            //resultModel!.MailingAddressProvince.ShouldBe(expectedMailingAddressProvince);
            //resultModel!.MailingAddressPostalCode.ShouldBe(expectedMailingAddressPostalCode);
        }
    }
}

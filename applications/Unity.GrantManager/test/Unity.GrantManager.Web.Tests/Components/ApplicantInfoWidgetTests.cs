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
            var applicationDto = new GrantApplicationDto()
            {
                ContactFullName = "John Doe",
                ContactTitle = "Doctor",
                ContactEmail = "john.doe@email.com",
                ContactBusinessPhone = "+12501234567",
                ContactCellPhone = "+12501234567",
                SigningAuthorityFullName = "Sam D",
                SigningAuthorityTitle ="Director",
                SigningAuthorityEmail = "sam.d@email.com",
                SigningAuthorityBusinessPhone = "+12501234566",
                SigningAuthorityCellPhone = "+12501234566",

            };

            // Arrange
            var appService = Substitute.For<IGrantApplicationAppService>();
            appService.GetAsync(Arg.Any<Guid>()).Returns(applicationDto);
            var sectorService = Substitute.For<ISectorService>();
            var viewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ApplicantInfoViewComponent(appService, sectorService)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            //Act
            var result = await viewComponent.InvokeAsync(Guid.NewGuid()) as ViewViewComponentResult;
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

            resultModel!.ApplicantInfo!.ContactFullName.ShouldBe(expectedFullName);
            resultModel!.ApplicantInfo!.ContactTitle.ShouldBe(expectedTitle);
            resultModel!.ApplicantInfo!.ContactEmail.ShouldBe(expectedEmail);
            resultModel!.ApplicantInfo!.ContactBusinessPhone.ShouldBe(expectedBusinessPhone);
            resultModel!.ApplicantInfo!.ContactCellPhone.ShouldBe(expectedCellPhone);
            resultModel!.ApplicantInfo!.SigningAuthorityFullName.ShouldBe(expectedSigningAuthorityFullName);
            resultModel!.ApplicantInfo!.SigningAuthorityTitle.ShouldBe(expectedSigningAuthorityTitle);
            resultModel!.ApplicantInfo!.SigningAuthorityEmail.ShouldBe(expectedSigningAuthorityEmail);
            resultModel!.ApplicantInfo!.SigningAuthorityBusinessPhone.ShouldBe(expectedSigningAuthorityBusinessPhone);
            resultModel!.ApplicantInfo!.SigningAuthorityCellPhone.ShouldBe(expectedSigningAuthorityCellPhone);
        }
    }
}

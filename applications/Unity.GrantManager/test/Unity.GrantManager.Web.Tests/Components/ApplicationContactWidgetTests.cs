using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicationContactsWidget;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class ApplicationContactWidgetTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task ApplicationContactWidgetReturnsStatus()
        {
            // Arrange
            var applicationContactService = Substitute.For<IApplicationContactService>();
            var applicationId = Guid.NewGuid();
            var expectedContactType = "ContactType";
            var expectedContactFullName = "ContactFullName";
            var expectedContactEmail = "ContactEmail";
            var expectedContactMobilePhone = "ContactMobilePhone";
            var expectedContactWorkPhone = "ContactWorkPhone";
            var expectedContactTitle = "ContactTitle";
            List<ApplicationContactDto> applicationContactDtos = new List<ApplicationContactDto>();
            applicationContactDtos.Add(new ApplicationContactDto()
                {
                    ApplicationId = applicationId,
                    ContactType = expectedContactType,
                    ContactFullName = expectedContactFullName,
                    ContactEmail = expectedContactEmail,
                    ContactMobilePhone = expectedContactMobilePhone,
                    ContactWorkPhone = expectedContactWorkPhone,
                    ContactTitle = expectedContactTitle
                });
            var httpContext = new DefaultHttpContext();

            applicationContactService.GetListAsync(applicationId).Returns(await Task.FromResult(applicationContactDtos));

            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ApplicationContactsWidgetViewComponent(applicationContactService)
            {
                ViewComponentContext = viewComponentContext
            };

            //Act
            var result = await viewComponent.InvokeAsync(applicationId, true) as ViewViewComponentResult;
            ApplicationContactsWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ApplicationContactsWidgetViewModel;

            //Assert
            resultModel!.ApplicationContacts.First().ContactFullName.ShouldBe(expectedContactFullName);
            resultModel!.ApplicationContacts.First().ContactType.ShouldBe(expectedContactType);
            resultModel!.ApplicationContacts.First().ContactTitle.ShouldBe(expectedContactTitle);
            resultModel!.ApplicationContacts.First().ContactEmail.ShouldBe(expectedContactEmail);
            resultModel!.ApplicationContacts.First().ContactMobilePhone.ShouldBe(expectedContactMobilePhone);
            resultModel!.ApplicationContacts.First().ContactWorkPhone.ShouldBe(expectedContactWorkPhone);
        }
    }
}

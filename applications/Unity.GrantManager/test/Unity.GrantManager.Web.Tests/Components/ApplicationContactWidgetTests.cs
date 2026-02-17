using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicationContactsWidget;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class ApplicationContactWidgetTests : GrantManagerWebTestBase
    {
        public ApplicationContactWidgetTests()
        {
            // Disable logging to avoid disposed logger errors during tests
            Environment.SetEnvironmentVariable("Logging:LogLevel:Default", "None");
        }

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
            var applicationContactDtos = new List<ApplicationContactDto>
            {
                new()
                {
                    ApplicationId = applicationId,
                    ContactType = expectedContactType,
                    ContactFullName = expectedContactFullName,
                    ContactEmail = expectedContactEmail,
                    ContactMobilePhone = expectedContactMobilePhone,
                    ContactWorkPhone = expectedContactWorkPhone,
                    ContactTitle = expectedContactTitle
                }
            };
            var httpContext = new DefaultHttpContext();

            applicationContactService.GetListByApplicationAsync(applicationId).Returns(await Task.FromResult(applicationContactDtos));

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
            var result = await viewComponent.InvokeAsync(applicationId) as ViewViewComponentResult;
            ApplicationContactsWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ApplicationContactsWidgetViewModel;

            //Assert
            resultModel!.ApplicationContacts[0].ContactFullName.ShouldBe(expectedContactFullName);
            resultModel!.ApplicationContacts[0].ContactType.ShouldBe(expectedContactType);
            resultModel!.ApplicationContacts[0].ContactTitle.ShouldBe(expectedContactTitle);
            resultModel!.ApplicationContacts[0].ContactEmail.ShouldBe(expectedContactEmail);
            resultModel!.ApplicationContacts[0].ContactMobilePhone.ShouldBe(expectedContactMobilePhone);
            resultModel!.ApplicationContacts[0].ContactWorkPhone.ShouldBe(expectedContactWorkPhone);
        }
    }
}

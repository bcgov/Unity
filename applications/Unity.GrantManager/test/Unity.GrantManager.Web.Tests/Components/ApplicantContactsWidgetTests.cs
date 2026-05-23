using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicantContacts;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace Unity.GrantManager.Components
{
    [Collection(WebTestCollection.Name)]
    public class ApplicantContactsWidgetTests
    {
        [Fact]
        public async Task ApplicantContactsWidgetReturnsExpectedModel()
        {
            var applicantContactQueryService = Substitute.For<IApplicantContactQueryService>();
            var permissionChecker = Substitute.For<IPermissionChecker>();
            var applicantId = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            applicantContactQueryService.GetByApplicantIdAsync(applicantId).Returns(new ApplicantContactInfoDto
            {
                Contacts = new List<ContactInfoItemDto>
                {
                    new()
                    {
                        ContactId = contactId,
                        Name = "Pat Doe",
                        Title = "Director",
                        Email = "pat@example.com",
                        WorkPhoneNumber = "555-1111",
                        MobilePhoneNumber = "555-2222",
                        Role = "Primary",
                        ContactType = "Applicant",
                        IsPrimary = true,
                        IsEditable = true,
                        CreationTime = DateTime.UtcNow
                    }
                }
            });

            permissionChecker.IsGrantedAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

            var httpContext = new DefaultHttpContext();
            var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ApplicantContactsViewComponent(applicantContactQueryService, permissionChecker)
            {
                ViewComponentContext = viewComponentContext
            };

            var result = await viewComponent.InvokeAsync(applicantId) as ViewViewComponentResult;
            var model = result!.ViewData!.Model as ApplicantContactsViewModel;

            model.ShouldNotBeNull();
            model.ApplicantId.ShouldBe(applicantId);
            model.CanEditContact.ShouldBeTrue();
            model.Contacts.Count.ShouldBe(1);
            model.Contacts[0].ContactId.ShouldBe(contactId);
            model.PrimaryContact.ShouldNotBeNull();
            model.PrimaryContact.FullName.ShouldBe("Pat Doe");
            model.PrimaryContact.Email.ShouldBe("pat@example.com");
        }
    }
}

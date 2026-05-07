using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Authorization.Permissions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantContacts
{
    [Widget(
        RefreshUrl = "Widget/ApplicantContacts/Refresh",
        ScriptTypes = new[] { typeof(ApplicantContactsScriptBundleContributor) },
        StyleTypes = new[] { typeof(ApplicantContactsStyleBundleContributor) },
        AutoInitialize = true)]
    public class ApplicantContactsViewComponent(
        IApplicantContactQueryService applicantContactQueryService,
        IPermissionChecker permissionChecker) : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                return View(new ApplicantContactsViewModel { ApplicantId = applicantId });
            }

            var aggregated = await applicantContactQueryService.GetByApplicantIdAsync(applicantId);

            var contacts = aggregated.Contacts
                .OrderByDescending(c => c.IsPrimary)
                .ThenByDescending(c => c.CreationTime)
                .ToList();

            var viewModel = new ApplicantContactsViewModel
            {
                ApplicantId = applicantId,
                CanEditContact = await permissionChecker.IsGrantedAsync(UnitySelector.Applicant.Contact.Update),
                Contacts = contacts
            };

            var primary = contacts.FirstOrDefault(c => c.IsPrimary);
            if (primary != null)
            {
                viewModel.PrimaryContact = new ApplicantPrimaryContactDisplayModel
                {
                    ContactId = primary.ContactId,
                    FullName = primary.Name ?? string.Empty,
                    Title = primary.Title ?? string.Empty,
                    Email = primary.Email ?? string.Empty,
                    WorkPhone = primary.WorkPhoneNumber ?? string.Empty,
                    MobilePhone = primary.MobilePhoneNumber ?? string.Empty
                };
            }

            return View(viewModel);
        }
    }

    public class ApplicantContactsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantContacts/Default.css");
        }
    }

    public class ApplicantContactsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
                .AddIfNotContains("/Views/Shared/Components/ApplicantContacts/Default.js");
        }
    }
}

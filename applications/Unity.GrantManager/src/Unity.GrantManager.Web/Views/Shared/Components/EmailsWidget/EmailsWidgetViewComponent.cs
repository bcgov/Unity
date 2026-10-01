using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.Applications;
using Unity.GrantManager.ApplicantProfile;
using Unity.Notifications.Emails;
using Unity.Notifications.Permissions;
using Volo.Abp.Settings;
using Unity.Notifications.Settings;
using System.Threading.Tasks;
using Unity.Notifications.Templates;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailsWidget
{
    [Widget(
        RefreshUrl = "Widgets/Emails/RefreshEmails",
        ScriptTypes = [typeof( EmailsWidgetScriptBundleContributor)],
        StyleTypes = [typeof( EmailsWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class EmailsWidgetViewComponent(ISettingProvider settingProvider, IApplicationRepository applicationRepository, ITemplateService templateService, IApplicantContactQueryService contactQueryService, EmailComposerAccessChecker emailAccessChecker) : AbpViewComponent
    {
       
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid currentUserId, bool noDraftPreviewMode = false, Guid applicantId = default)
        {
            string emailTo;
            if (applicantId != Guid.Empty)
            {
                await emailAccessChecker.CheckOwnerAsync(applicationId, applicantId, NotificationsPermissions.Email.Default);
                var contacts = await contactQueryService.GetByApplicantIdAsync(applicantId);
                emailTo = contacts.Contacts.Where(c => c.IsPrimary).OrderByDescending(c => c.CreationTime).FirstOrDefault()?.Email ?? string.Empty;
            }
            else
            {
                var application = await applicationRepository.WithBasicDetailsAsync(applicationId);
                emailTo = application?.ApplicantAgent?.Email ?? string.Empty;
            }

            var defaultFromAddress = await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
            var enableEmailDelay = string.Equals(
                await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.EnableEmailDelay),
                "true", StringComparison.OrdinalIgnoreCase);

            EmailsWidgetViewModel model = new()
            {
                ApplicationId = applicationId,
                ApplicantId = applicantId,
                CurrentUserId = currentUserId,
                EmailTo = emailTo,
                EmailFrom = defaultFromAddress ?? "NoReply@gov.bc.ca",
                EnableEmailDelay = enableEmailDelay,
                NoDraftPreviewMode = noDraftPreviewMode
            };
            await PopulateTemplates(model);

            return View(model);
        }
        private async Task PopulateTemplates(EmailsWidgetViewModel model)
        {
            var templates = (await templateService.GetTemplatesByTenant())
                .Where(t => string.Equals(t.TemplateType, model.TemplateType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            templates.ForEach(t =>
           {
               model.TemplatesList.Add(new SelectListItem() { Value = t.Id.ToString(), Text = t.Name });

           });
        }
                   
    }

    public class  EmailsWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/EmailsWidget/Default.css");
            context.Files.AddIfNotContains("/libs/select2/dist/css/select2.min.css");
        }
    }

    public class  EmailsWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/EmailsWidget/Default.js");
            context.Files.AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
            context.Files.AddIfNotContains("/libs/select2/dist/js/select2.full.min.js");
        }
    }
}

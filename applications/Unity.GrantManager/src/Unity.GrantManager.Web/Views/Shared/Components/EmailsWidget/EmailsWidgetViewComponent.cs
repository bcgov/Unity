using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.Applications;
using Volo.Abp.Settings;
using Unity.Notifications.Settings;
using System.Threading.Tasks;
using Unity.Notifications.Templates;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailsWidget
{
    [Widget(
        RefreshUrl = "Widgets/Emails/RefreshEmails",
        ScriptTypes = [typeof( EmailsWidgetScriptBundleContributor)],
        StyleTypes = [typeof( EmailsWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class EmailsWidgetViewComponent(ISettingProvider settingProvider, IApplicationRepository applicationRepository, ITemplateService templateService) : AbpViewComponent
    {
       
        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid currentUserId)
        {
            // Lookup the applicant contact
            Application application = await applicationRepository.WithBasicDetailsAsync(applicationId);

            var defaultFromAddress = await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
            EmailsWidgetViewModel model = new()
            {
                ApplicationId = applicationId,
                CurrentUserId = currentUserId,
                EmailTo = application?.ApplicantAgent?.Email ?? string.Empty,
                EmailFrom = defaultFromAddress ?? "NoReply@gov.bc.ca",
            };
            await PopulateTemplates(model);

            return View(model);
        }
        private async Task PopulateTemplates(EmailsWidgetViewModel model)
        {
            var templates = await templateService.GetTemplatesByTenent();

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
        }
    }

    public class  EmailsWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files.AddIfNotContains("/Views/Shared/Components/EmailsWidget/Default.js");
            context.Files.AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Settings;
using Volo.Abp.AspNetCore.Mvc.UI.Alerts;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Unity.GrantManager.Web.Pages.Settings.UserSettings
{
    public partial class IndexModel(ILogger<IndexModel> logger,
        IAlertManager alertManager,
        ISettingProvider settingProvider) : GrantManagerPageModel
    {
        [BindProperty]
        public bool EmailBlockCommentTagging { get; set; }

        [BindProperty]
        public bool EmailBlockAssigned { get; set; }


        private async Task LoadUserSettingsAsync()
        {
            EmailBlockCommentTagging = await settingProvider.GetAsync(
                SettingsConstants.UserEmailSettings.EmailBlockCommentTagging,
                defaultValue: true);

            EmailBlockAssigned = await settingProvider.GetAsync(
                SettingsConstants.UserEmailSettings.EmailBlockAssigned,
                defaultValue: true);

            alertManager.Alerts.Clear();
        }

        public async Task<IActionResult> OnPostSettingsAsync(
            [FromServices] ISettingManager settingManager)
        {
            if (EmailBlockCommentTagging)
            {
                ModelState.Remove(nameof(EmailBlockCommentTagging));
            }

            if (EmailBlockAssigned)
            {
                ModelState.Remove(nameof(EmailBlockAssigned));
            }

            if (!ModelState.IsValid)
            {
                alertManager.Alerts.Danger("Please correct the validation errors.");
                await LoadUserSettingsAsync();
                return Page();
            }

            try
            {
                await settingManager.SetForCurrentUserAsync(
                    SettingsConstants.UserEmailSettings.EmailBlockAssigned,
                    EmailBlockAssigned.ToString().ToLowerInvariant());

                await settingManager.SetForCurrentUserAsync(
                    SettingsConstants.UserEmailSettings.EmailBlockCommentTagging,
                    EmailBlockCommentTagging.ToString().ToLowerInvariant());

                alertManager.Alerts.Success("Your settings have been saved successfully.");
                return Page();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving user settings.");
                alertManager.Alerts.Danger("Unable to save your settings. Please try again.");
                await LoadUserSettingsAsync();
                return Page();
            }
        }

        public async Task OnGetAsync()
        {
            await LoadUserSettingsAsync();
        }
    }
}
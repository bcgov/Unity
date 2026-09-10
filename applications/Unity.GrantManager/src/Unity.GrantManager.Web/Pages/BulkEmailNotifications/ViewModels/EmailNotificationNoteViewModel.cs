using Microsoft.Extensions.Localization;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Pages.BulkEmailNotifications.ViewModels
{
    public class EmailNotificationNoteViewModel
    {
        public EmailNotificationNoteViewModel(string key, bool active, string description, bool isError)
        {
            Key = key;
            Active = active;
            Description = description;
            IsError = isError;
        }

        public string Key { get; set; }
        public bool Active { get; set; }
        public string Description { get; set; }
        public bool IsError { get; set; }

        public static List<EmailNotificationNoteViewModel> CreateNotesList(IStringLocalizer localizer)
        {
            return
            [
                new("NO_DRAFT_FOUND", false, localizer.GetString("SendEmailNotificationRequest:NoDraftFound"), true),
                new("MULTIPLE_DRAFTS_FOUND", false, localizer.GetString("SendEmailNotificationRequest:MultipleDraftsFound"), true),
                new("MISSING_SUBJECT", false, localizer.GetString("SendEmailNotificationRequest:MissingSubject"), true),
                new("MISSING_TO_ADDRESS", false, localizer.GetString("SendEmailNotificationRequest:MissingToAddress"), true),
                new("MISSING_FROM_ADDRESS", false, localizer.GetString("SendEmailNotificationRequest:MissingFromAddress"), true),
                new("MISSING_BODY", false, localizer.GetString("SendEmailNotificationRequest:MissingBody"), true)
            ];
        }
    }
}

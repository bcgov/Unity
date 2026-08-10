using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Pages.BulkEmailNotifications.ViewModels
{
    public class BulkEmailNotificationViewModel
    {
        public BulkEmailNotificationViewModel()
        {
            Notes = [];
        }

        public Guid ApplicationId { get; set; }
        public Guid? EmailId { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string? ApplicantName { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public string? EmailSubject { get; set; }
        public bool IsValid { get; set; }
        public List<EmailNotificationNoteViewModel> Notes { get; set; }
    }
}

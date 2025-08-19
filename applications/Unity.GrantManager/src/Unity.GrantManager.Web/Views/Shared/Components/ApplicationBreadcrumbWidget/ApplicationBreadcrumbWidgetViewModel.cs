using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationBreadcrumbWidget
{
    public class ApplicationBreadcrumbWidgetViewModel
    {        
        public string ReferenceNo { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;

        public Guid ApplicationFormId { get; set; } = Guid.Empty;
        public string ApplicationFormName { get; set; } = string.Empty;
        public string ApplicationFormCategory { get; set; } = string.Empty;
        public Guid ApplicationFormVersionId { get; set; } = Guid.Empty;
        public int ApplicationFormVersion { get; set; } = 0;
        public string SubmissionFormDescription = string.Empty;
    }
}

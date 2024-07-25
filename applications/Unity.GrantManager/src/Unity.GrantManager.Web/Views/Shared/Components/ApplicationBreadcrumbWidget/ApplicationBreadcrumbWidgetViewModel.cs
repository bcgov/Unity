using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationBreadcrumbWidget
{
    public class ApplicationBreadcrumbWidgetViewModel
    {        
        public string ReferenceNo { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public int ApplicationFormVersion { get; set; } = 0;
    }
}

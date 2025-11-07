using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantBreadcrumbWidget
{
    public class ApplicantBreadcrumbWidgetViewModel
    {
        public Guid ApplicantId { get; set; }
        public string UnityApplicantId { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
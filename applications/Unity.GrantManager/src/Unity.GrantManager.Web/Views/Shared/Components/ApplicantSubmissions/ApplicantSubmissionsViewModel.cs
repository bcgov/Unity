using System;
using System.Collections.Generic;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantSubmissions
{
    public class ApplicantSubmissionsViewModel
    {
        public Guid ApplicantId { get; set; }
        public List<GrantApplicationDto> Submissions { get; set; } = new();
    }
}

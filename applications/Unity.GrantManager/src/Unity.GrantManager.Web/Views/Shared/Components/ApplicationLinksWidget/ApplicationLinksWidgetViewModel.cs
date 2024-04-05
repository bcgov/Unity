using System;
using System.Collections.Generic;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationLinksWidget
{
    public class ApplicationLinksWidgetViewModel
    {
        public ApplicationLinksWidgetViewModel()
        {
            ApplicationLinks = new List<ApplicationLinksInfoDto>();
        }

        public List<ApplicationLinksInfoDto> ApplicationLinks { get; set; }
        public Guid ApplicationId { get; set; }
    }
}

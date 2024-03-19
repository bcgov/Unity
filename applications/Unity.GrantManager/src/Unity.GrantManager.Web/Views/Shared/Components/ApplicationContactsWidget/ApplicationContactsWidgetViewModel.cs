using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationContactsWidget
{
    public class ApplicationContactsWidgetViewModel
    {
        public ApplicationContactsWidgetViewModel()
        {
            ApplicationContacts = new List<ApplicationContactDto>();
        }

        public List<ApplicationContactDto> ApplicationContacts { get; set; }
        public Guid ApplicationId { get; set; }
        public Boolean IsReadOnly { get; set; }

        public String ContactTypeValue(String contactType)
        {
            return ApplicationContactOptionList.ContactTypeList.FirstOrDefault(c => c.Key == contactType).Value;
        } 
    }
}

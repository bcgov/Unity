using Newtonsoft.Json.Linq;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications
{
    public class ApplicationFormManager : DomainService, IApplicationFormManager
    {
        public ApplicationForm SynchronizePublishedForm(ApplicationForm applicationForm, 
            dynamic formVersion, 
            dynamic form)
        {
            if (applicationForm.Version == null && formVersion != null)
            {
                var version = ((JObject)formVersion!).SelectToken("version");
                var published = ((JObject)formVersion!).SelectToken("published");

                if (published != null && published.ToString() == "True" 
                    && version != null 
                    && applicationForm.ChefsApplicationFormGuid != null)
                {
                    JObject formObject = JObject.Parse(form.ToString());
                    var formName = formObject.SelectToken("name");
                    var formDescription = formObject.SelectToken("description");
                    
                    if (formName != null)
                    {
                        applicationForm.ApplicationFormName = formName.ToString();
                    }

                    if (formDescription != null && string.IsNullOrWhiteSpace(applicationForm.ApplicationFormDescription))
                    {
                        applicationForm.ApplicationFormDescription = formDescription.ToString();
                    }

                    applicationForm.Version = int.Parse(version.ToString());
                }
            }

            return applicationForm;
        }
    }
}

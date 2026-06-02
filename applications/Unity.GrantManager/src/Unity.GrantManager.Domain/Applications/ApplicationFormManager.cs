using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Unity.GrantManager.Intakes;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications
{
    public class ApplicationFormManager : DomainService, IApplicationFormManager
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IIntakeRepository _intakeRepository;

        public ApplicationFormManager(IIntakeRepository intakeRepository, 
            IApplicationFormRepository applicationFormRepository)
        {
            _intakeRepository = intakeRepository;
            _applicationFormRepository = applicationFormRepository;
        }

        public async Task<ApplicationForm> InitializeApplicationForm(EventSubscription eventSubscription)
        {
            Intake? intake = await _intakeRepository.FirstOrDefaultAsync();
            var applicationForm = new ApplicationForm();
            if (intake != null)
            {

                applicationForm = await _applicationFormRepository.InsertAsync(
                                new ApplicationForm
                                {
                                    IntakeId = intake.Id,
                                    ApplicationFormName = "New Form - Setup API KEY",
                                    ChefsApplicationFormGuid = eventSubscription.FormId.ToString(),
                                    ChefsCriteriaFormGuid = ""
                                },
                                autoSave: true
                            );

            }

            return applicationForm;
        }

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
                    
                    if(formName != null)
                    {
                        applicationForm.ApplicationFormName = formName.ToString();
                    }

                    if (formDescription != null && applicationForm.ApplicationFormDescription == null)
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

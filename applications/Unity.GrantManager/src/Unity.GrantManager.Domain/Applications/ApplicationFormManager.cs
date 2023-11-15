using System;
using System.Threading.Tasks;
using Unity.GrantManager.Events;
using Volo.Abp.Domain.Services;
using Unity.GrantManager.Intakes;
using Volo.Abp.Domain.Repositories;
using Newtonsoft.Json.Linq;

namespace Unity.GrantManager.Applications
{
    public class ApplicationFormManager : DomainService, IApplicationFormManager
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IIntakeRepository _intakeRepository;

        private readonly IFormAppService _formAppService;

        public ApplicationFormManager(IIntakeRepository intakeRepository, 
            IApplicationFormRepository applicationFormRepository,            
            IFormAppService formAppService)
        {
            _intakeRepository = intakeRepository;
            _applicationFormRepository = applicationFormRepository;
            _formAppService = formAppService;
        }

        public async Task<ApplicationForm> InitializeApplicationForm(EventSubscriptionDto eventSubscriptionDto)
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
                                    ChefsApplicationFormGuid = eventSubscriptionDto.FormId.ToString(),
                                    ChefsCriteriaFormGuid = ""
                                },
                                autoSave: true
                            );

            }

            return applicationForm;
        }

        public async Task<ApplicationForm> SynchronizePublishedForm(ApplicationForm applicationForm, dynamic formVersion)
        {
            if (applicationForm.Version == null && formVersion != null)
            {
                var version = ((JObject)formVersion!).SelectToken("version");                
                var published = ((JObject)formVersion!).SelectToken("published");

                if (published != null && ((JToken)published).ToString() == "True" 
                    && version != null 
                    && applicationForm.ChefsApplicationFormGuid != null)
                {

                    dynamic form = await _formAppService.GetForm(Guid.Parse(applicationForm.ChefsApplicationFormGuid));
                    JObject formObject = JObject.Parse(form.ToString());
                    var formName = formObject.SelectToken("name");
                    var formDescription = formObject.SelectToken("description");
                    if(formName != null && formDescription != null)
                    {
                        applicationForm.ApplicationFormName = formName.ToString();
                        applicationForm.ApplicationFormDescription = formDescription.ToString();
                    }

                    applicationForm.Version = int.Parse(version.ToString());
                }
            }

            return applicationForm;
        }
    }
}

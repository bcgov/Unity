using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integrations.Chefs;
using Unity.GrantManager.Notifications;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Events
{
    [RemoteService(false)]
    public class ChefsEventSubscriptionService(
            INotificationsAppService notificationsAppService,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IApplicationFormManager applicationFormManager,
            IFormsApiService submissionsIntService,
            IApplicationFormRepository applicationFormRepository,
            IFormsApiService formsApiService,
            IApplicationFormVersionAppService applicationFormVersionAppService) : GrantManagerAppService, IChefsEventSubscriptionService
    {

        public async Task<bool> CreateIntakeMappingAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            _ = (await applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");
    
            var submissionData = await submissionsIntService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId)
                ?? throw new InvalidFormDataSubmissionException();

            // Access the 'submission' property from the JObject using the appropriate key.
            var submission = submissionData["submission"]
                ?? throw new InvalidFormDataSubmissionException();

            // Ensure the 'formVersionId' is accessed correctly from the 'submission' object.
            var formVersionId = submission["formVersionId"]?.ToString()
                ?? throw new InvalidFormDataSubmissionException();

            var formVersion = await formsApiService.GetFormDataAsync(eventSubscriptionDto.FormId.ToString(), formVersionId)
                ?? throw new InvalidFormDataSubmissionException();
            var result = intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);
            return !result.IsNullOrEmpty();
        }

        public async Task<bool> PublishedFormAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            string formId = eventSubscriptionDto.FormId.ToString();
            string formVersionId = eventSubscriptionDto.FormVersion.ToString();

            var applicationForm = (await applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == formId)
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm != null
                && applicationForm.ApiKey != null
                && applicationForm.ChefsApplicationFormGuid != null)
            {
                // Go grab the new name/description/version and map the new available fields
                var formVersion = await formsApiService.GetFormDataAsync(formId, formVersionId);
                if (formVersion == null)
                {
                    throw new InvalidFormDataSubmissionException("Form version data is null.");
                }

                dynamic form = await formsApiService.GetForm(Guid.Parse(applicationForm.ChefsApplicationFormGuid), applicationForm.ChefsApplicationFormGuid.ToString(), applicationForm.ApiKey);
                if (form == null)
                {
                    throw new InvalidFormDataSubmissionException("Form data is null.");
                }

                applicationForm = applicationFormManager.SynchronizePublishedForm(applicationForm, formVersion, form);
                await applicationFormVersionAppService.UpdateOrCreateApplicationFormVersion(formId, formVersionId, applicationForm.Id, formVersion);
                applicationForm = await applicationFormRepository.UpdateAsync(applicationForm);
                notificationsAppService.PostChefsEventToTeamsAsync(eventSubscriptionDto.SubscriptionEvent, form, formVersion);
            }
            else if (applicationForm == null)
            {
                EventSubscription eventSubscription = ObjectMapper.Map<EventSubscriptionDto, EventSubscription>(eventSubscriptionDto);
                applicationForm = await applicationFormManager.InitializeApplicationForm(eventSubscription);
            }

            return applicationForm != null;
        }
    }
}

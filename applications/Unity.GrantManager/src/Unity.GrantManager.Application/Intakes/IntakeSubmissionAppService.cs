using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Integrations.Chefs;
using Unity.GrantManager.Notifications;

using Volo.Abp;

namespace Unity.GrantManager.Intakes
{
    [RemoteService(false)]
    public class IntakeSubmissionAppService(INotificationsAppService notificationsAppService,
                                            IIntakeFormSubmissionManager intakeFormSubmissionManager,
                                            IFormsApiService formsApiService,
                                            IApplicationFormRepository applicationFormRepository,
                                            IApplicationFormVersionAppService applicationFormVersionAppService) : GrantManagerAppService, IIntakeSubmissionAppService
    {

        public async Task<EventSubscriptionConfirmationDto> CreateIntakeSubmissionAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new ApplicationFormSetupException("Application Form Not Registered");

            JObject submissionData = await formsApiService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId) ?? throw new InvalidFormDataSubmissionException();

            bool validSubmission = await ValidateSubmission(eventSubscriptionDto, submissionData);
            if (!validSubmission) {
                return new EventSubscriptionConfirmationDto() { ExceptionMessage = "An Error Occured Validating the Chefs Submission" };
            } 
            
            JToken? token = submissionData.SelectToken("submission.formVersionId");
            if (token != null)
            {
                await StoreChefsFieldMappingAsync(eventSubscriptionDto, applicationForm, token);
            }
           
            var result = await intakeFormSubmissionManager.ProcessFormSubmissionAsync(applicationForm, submissionData);
            return new EventSubscriptionConfirmationDto() { ConfirmationId = result };
        }

        private async Task<bool> ValidateSubmission(EventSubscriptionDto eventSubscriptionDto, JObject submissionData)
        {
            JToken? tokenDraft = submissionData.SelectToken("submission.draft");
            
            if (tokenDraft != null && tokenDraft.ToString() == "True") {
                string factName = "A draft submission was submitted and should not have been";
                string factValue = $"FormId: {eventSubscriptionDto.FormId} SubmissionID: {eventSubscriptionDto.SubmissionId}";
                await notificationsAppService.NotifyChefsEventToTeamsAsync(factName, factValue, true);
                return false;
            }

            JToken? tokenDeleted = submissionData.SelectToken("submission.deleted");
            if (tokenDeleted != null && tokenDeleted.ToString() == "True") {
                string factName = "A deleted submission was submitted - user navigated back and got a success message from chefs";
                string factValue = $"FormId: {eventSubscriptionDto.FormId} SubmissionID: {eventSubscriptionDto.SubmissionId}";
                await  notificationsAppService.NotifyChefsEventToTeamsAsync(factName, factValue, false);
            }

            // If there are no mappings initialize the available
            bool formVersionExists = await applicationFormVersionAppService.FormVersionExists(eventSubscriptionDto.FormVersion.ToString());

            if (!formVersionExists)
            {
                dynamic? formVersion = await formsApiService.GetFormDataAsync(eventSubscriptionDto.FormId.ToString(),
                    eventSubscriptionDto.FormVersion.ToString());

                if(formVersion == null)
                {
                    string factName = "Application Form Version Not Registered - Unknown Version";
                    string factValue = $"FormId: {eventSubscriptionDto.FormId} FormVersion: {eventSubscriptionDto.FormVersion}";
                    await notificationsAppService.NotifyChefsEventToTeamsAsync(factName, factValue, true);
                    return false;
                } else {
                    var version = ((JObject)formVersion!).SelectToken("version");
                    var published = ((JObject)formVersion!).SelectToken("published");
                    string factName = "Application Form Version Not Registered - Unknown Version";
                    string factValue = $"Application Form Version Not Registerd - Version: {version} Published: {published}";
                    await notificationsAppService.NotifyChefsEventToTeamsAsync(factName, factValue, true);
                    return false;
                }
            }
            return true;
        }

        private async Task StoreChefsFieldMappingAsync(EventSubscriptionDto eventSubscriptionDto, ApplicationForm applicationForm, JToken token)
        {
            Guid formVersionId = Guid.Parse(token.ToString());
            var formData = await formsApiService.GetFormDataAsync(eventSubscriptionDto.FormId.ToString(), formVersionId.ToString()) ?? throw new InvalidFormDataSubmissionException();
            string chefsFormId = eventSubscriptionDto.FormId.ToString();
            string chefsFormVersionId = eventSubscriptionDto.FormVersion.ToString();
            await applicationFormVersionAppService.UpdateOrCreateApplicationFormVersion(chefsFormId, chefsFormVersionId, applicationForm.Id, formData);
        }
    }
}

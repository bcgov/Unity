using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Intake;
using Unity.GrantManager.Integration.Chefs;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp;

namespace Unity.GrantManager.Intakes
{
    [RemoteService(false)]
    public class IntakeSubmissionAppService : GrantManagerAppService, IIntakeSubmissionAppService
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IIntakeFormSubmissionManager _intakeFormSubmissionManager;
        private readonly IFormsApiService _formsApiService;
        private readonly ISubmissionsApiService _submissionsIntService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IOptions<IntakeClientOptions> _intakeClientOptions;
        private readonly IConfiguration _configuration;

        public IntakeSubmissionAppService(IIntakeFormSubmissionManager intakeFormSubmissionManager,
            IFormsApiService formsApiService,
            ISubmissionsApiService submissionsIntService,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IOptions<IntakeClientOptions> intakeClientOptions,
            IConfiguration configuration)
        {
            _intakeFormSubmissionManager = intakeFormSubmissionManager;
            _submissionsIntService = submissionsIntService;
            _applicationFormRepository = applicationFormRepository;
            _formsApiService = formsApiService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _intakeClientOptions = intakeClientOptions;
            _configuration = configuration;
        }

        public async Task<EventSubscriptionConfirmationDto> CreateIntakeSubmissionAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new ApplicationFormSetupException("Application Form Not Registered");

            JObject submissionData = await _submissionsIntService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId) ?? throw new InvalidFormDataSubmissionException();

            bool validSubmission = await ValidateSubmission(eventSubscriptionDto, submissionData);
            if (!validSubmission) {
                return new EventSubscriptionConfirmationDto() { ExceptionMessage = "An Error Occured Validating the Chefs Submission" };
            } 
            
            JToken? token = submissionData.SelectToken("submission.formVersionId");
            if (token != null)
            {
                await StoreChefsFieldMappingAsync(eventSubscriptionDto, applicationForm, token);
            }
           
            var result = await _intakeFormSubmissionManager.ProcessFormSubmissionAsync(applicationForm, submissionData);
            return new EventSubscriptionConfirmationDto() { ConfirmationId = result };
        }

        private async Task<bool> ValidateSubmission(EventSubscriptionDto eventSubscriptionDto, JObject submissionData)
        {
            JToken? tokenDraft = submissionData.SelectToken("submission.draft");
            
            if (tokenDraft != null && tokenDraft.ToString() == "True") {
                string factName = "A draft submission was submitted and should not have been";
                string factValue = $"FormId: {eventSubscriptionDto.FormId} SubmissionID: {eventSubscriptionDto.SubmissionId}";
                await SendTeamsNotification(factName, factValue);
                return false;
            }

            JToken? tokenDeleted = submissionData.SelectToken("submission.deleted");
            if (tokenDeleted != null && tokenDeleted.ToString() == "True") {
                string factName = "A deleted submission was submitted - user navigated back and got a success message from chefs";
                string factValue = $"FormId: {eventSubscriptionDto.FormId} SubmissionID: {eventSubscriptionDto.SubmissionId}";
                await SendTeamsNotification(factName, factValue);
            }

            // If there are no mappings initialize the available
            bool formVersionExists = await _applicationFormVersionAppService.FormVersionExists(eventSubscriptionDto.FormVersion.ToString());

            if (!formVersionExists)
            {
                dynamic? formVersion = await _formsApiService.GetFormDataAsync(eventSubscriptionDto.FormId.ToString(),
                    eventSubscriptionDto.FormVersion.ToString());

                if(formVersion == null)
                {
                    string factName = "Application Form Version Not Registered - Unknown Version";
                    string factValue = $"FormId: {eventSubscriptionDto.FormId} FormVersion: {eventSubscriptionDto.FormVersion}";
                    await SendTeamsNotification(factName, factValue);
                    return false;
                } else if(!_intakeClientOptions.Value.AllowUnregisteredVersions)
                {
                    var version = ((JObject)formVersion!).SelectToken("version");
                    var published = ((JObject)formVersion!).SelectToken("published");
                    string factName = "Application Form Version Not Registered - Unknown Version";
                    string factValue = $"Application Form Version Not Registerd - Version: {version} Published: {published}";
                    await SendTeamsNotification(factName, factValue);
                    return false;
                }
            }
            return true;
        }

        private async Task SendTeamsNotification(string factName, string factValue)
        {
            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string activityTitle = "Chefs Submission Event Validation Error";
            string activitySubtitle = "Environment: " + envInfo;
            string teamsChannel = _configuration["Notifications:TeamsNotificationsWebhook"] ?? "";
            TeamsNotificationService teamsNotificationService = new TeamsNotificationService();
            teamsNotificationService.AddFact(factName, factValue);
            await teamsNotificationService.PostFactsToTeamsAsync(teamsChannel, activityTitle, activitySubtitle);
        }

        private async Task StoreChefsFieldMappingAsync(EventSubscriptionDto eventSubscriptionDto, ApplicationForm applicationForm, JToken token)
        {
            Guid formVersionId = Guid.Parse(token.ToString());
            var formData = await _formsApiService.GetFormDataAsync(eventSubscriptionDto.FormId.ToString(), formVersionId.ToString()) ?? throw new InvalidFormDataSubmissionException();
            string chefsFormId = eventSubscriptionDto.FormId.ToString();
            string chefsFormVersionId = eventSubscriptionDto.FormVersion.ToString();
            await _applicationFormVersionAppService.UpdateOrCreateApplicationFormVersion(chefsFormId, chefsFormVersionId, applicationForm.Id, formData);
        }
    }
}

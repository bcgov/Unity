using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integration.Chefs;
using Unity.GrantManager.TeamsNotifications;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Events
{
    [RemoteService(false)]
    public class ChefsEventSubscriptionService : GrantManagerAppService, IChefsEventSubscriptionService
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationFormManager _applicationFormManager;
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly ISubmissionsApiService _submissionsIntService;
        private readonly IFormsApiService _formsApiService;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IConfiguration _configuration;

        public ChefsEventSubscriptionService(
            IConfiguration configuration,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IApplicationFormManager applicationFormManager,
            ISubmissionsApiService submissionsIntService,
            IApplicationFormRepository applicationFormRepository,
            IFormsApiService formsApiService,
            IApplicationFormVersionAppService applicationFormVersionAppService)
        {
            _configuration = configuration;
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _submissionsIntService = submissionsIntService;
            _applicationFormRepository = applicationFormRepository;
            _formsApiService = formsApiService;
            _applicationFormManager = applicationFormManager;
            _applicationFormVersionAppService = applicationFormVersionAppService;
        }

        public async Task<bool> CreateIntakeMappingAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

            var submissionData = await _submissionsIntService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId) ?? throw new InvalidFormDataSubmissionException();
            var formVersion = await _formsApiService.GetFormDataAsync(eventSubscriptionDto.FormId.ToString(), submissionData.submission.formVersionId.ToString()) ?? throw new InvalidFormDataSubmissionException();
            var result = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);
            return !result.IsNullOrEmpty();
        }

        public async Task<bool> PublishedFormAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            string formId = eventSubscriptionDto.FormId.ToString();
            string formVersionId = eventSubscriptionDto.FormVersion.ToString();

            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == formId)
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm != null
                && applicationForm.ApiKey != null
                && applicationForm.ChefsApplicationFormGuid != null)
            {
                // Go grab the new name/description/version and map the new available fields
                var formVersion = await _formsApiService.GetFormDataAsync(formId, formVersionId);
                dynamic form = await _formsApiService.GetForm(Guid.Parse(applicationForm.ChefsApplicationFormGuid), applicationForm.ChefsApplicationFormGuid.ToString(), applicationForm.ApiKey);
                applicationForm = _applicationFormManager.SynchronizePublishedForm(applicationForm, formVersion, form);
                await _applicationFormVersionAppService.UpdateOrCreateApplicationFormVersion(formId, formVersionId, applicationForm.Id, formVersion);
                applicationForm = await _applicationFormRepository.UpdateAsync(applicationForm);
                string teamsChannel = _configuration["Notifications:TeamsNotificationsChannelWebhook"] ?? "";
                TeamsNotificationService.PostChefsEventToTeamsAsync(teamsChannel, eventSubscriptionDto, form, formVersion);
            }
            else if(applicationForm == null)
            {
                EventSubscription eventSubscription = ObjectMapper.Map<EventSubscriptionDto, EventSubscription>(eventSubscriptionDto);
                applicationForm = await _applicationFormManager.InitializeApplicationForm(eventSubscription);
            }

            return applicationForm != null;
        }
    }
}

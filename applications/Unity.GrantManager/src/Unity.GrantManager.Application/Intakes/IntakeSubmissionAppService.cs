using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Integration.Chefs;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

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

        public IntakeSubmissionAppService(IIntakeFormSubmissionManager intakeFormSubmissionManager,
            IFormsApiService formsApiService,
            ISubmissionsApiService submissionsIntService,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormVersionAppService applicationFormVersionAppService)
        {
            _intakeFormSubmissionManager = intakeFormSubmissionManager;
            _submissionsIntService = submissionsIntService;
            _applicationFormRepository = applicationFormRepository;
            _formsApiService = formsApiService;
            _applicationFormVersionAppService = applicationFormVersionAppService;
        }

        public async Task<EventSubscriptionConfirmationDto> CreateIntakeSubmissionAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

            JObject submissionData = await _submissionsIntService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId) ?? throw new InvalidFormDataSubmissionException();

            // If there are no mappings initialize the available
            bool formVersionExists = await _applicationFormVersionAppService.FormVersionExists(eventSubscriptionDto.FormVersion.ToString());          
            if (formVersionExists)
            {
                JToken? token = submissionData.SelectToken("submission.formVersionId");
                if (token != null)
                {
                    await StoreChefsFieldMappingAsync(eventSubscriptionDto, applicationForm, token);
                }
            }

            var result = await _intakeFormSubmissionManager.ProcessFormSubmissionAsync(applicationForm, submissionData);
            return new EventSubscriptionConfirmationDto() { ConfirmationId = result };
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

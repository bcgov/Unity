using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Integration;
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
        private readonly ISubmissionsIntService _submissionsIntService;
        private readonly IFormIntService _formIntService;

        public ChefsEventSubscriptionService(IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IApplicationFormManager applicationFormManager,
            ISubmissionsIntService submissionsIntService,
            IApplicationFormRepository applicationFormRepository,
            IFormIntService formIntService)
        {
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _submissionsIntService = submissionsIntService;
            _applicationFormRepository = applicationFormRepository;
            _formIntService = formIntService;
            _applicationFormManager = applicationFormManager;
        }

        public async Task<bool> CreateIntakeMappingAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

            var submissionData = await _submissionsIntService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId) ?? throw new InvalidFormDataSubmissionException();
            var formVersion = await _formIntService.GetFormDataAsync(eventSubscriptionDto.FormId, submissionData.submission.formVersionId) ?? throw new InvalidFormDataSubmissionException();
            var result = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);
            return !result.IsNullOrEmpty();
        }

        public async Task<bool> PublishedFormAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm != null
                && applicationForm.ApiKey != null
                && applicationForm.ChefsApplicationFormGuid != null)
            {
                // Go grab the new name/description/version and map the new available fields
                var formVersion = await _formIntService.GetFormDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.FormVersion);
                dynamic form = await _formIntService.GetForm(Guid.Parse(applicationForm.ChefsApplicationFormGuid));
                applicationForm = _applicationFormManager.SynchronizePublishedForm(applicationForm, formVersion, form);

                applicationForm.AvailableChefsFields = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);
                applicationForm = await _applicationFormRepository.UpdateAsync(applicationForm);
            }
            else
            {
                EventSubscription eventSubscription = ObjectMapper.Map<EventSubscriptionDto, EventSubscription>(eventSubscriptionDto);
                applicationForm = await _applicationFormManager.InitializeApplicationForm(eventSubscription);
            }

            return applicationForm != null;
        }
    }
}

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
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly ISubmissionsIntService _submissionsIntService;
        private readonly IFormIntService _formIntService;

        public ChefsEventSubscriptionService(IIntakeFormSubmissionManager intakeFormSubmissionManager,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            ISubmissionsIntService submissionsIntService,
            IApplicationFormRepository applicationFormRepository,
            IFormIntService formIntService)
        {
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _submissionsIntService = submissionsIntService;
            _applicationFormRepository = applicationFormRepository;
            _formIntService = formIntService;
        }

        public async Task<EventSubscriptionConfirmationDto> CreateIntakeMappingAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

            var submissionData = await _submissionsIntService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId) ?? throw new InvalidFormDataSubmissionException();
            var formData = await _formIntService.GetFormDataAsync(eventSubscriptionDto.FormId, submissionData.submission.formVersionId) ?? throw new InvalidFormDataSubmissionException();
            var result = _intakeFormSubmissionMapper.InitializeAvailableFormFields(applicationForm, formData);

            return new EventSubscriptionConfirmationDto() { ConfirmationId = result };
        }
    }
}

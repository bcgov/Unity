using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Intakes.Integration;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Intakes
{
    [RemoteService(false)]
    public class IntakeSubmissionAppService : GrantManagerAppService, IIntakeSubmissionAppService
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IIntakeFormSubmissionManager _intakeFormSubmissionManager;
        private readonly IFormIntService _formIntService;
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly ISubmissionsIntService _submissionsIntService;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public IntakeSubmissionAppService(IIntakeFormSubmissionManager intakeFormSubmissionManager,
            IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IFormIntService formIntService,
            IUnitOfWorkManager unitOfWorkManager,
            ISubmissionsIntService submissionsIntService,
            IApplicationFormRepository applicationFormRepository)
        {
            _intakeFormSubmissionManager = intakeFormSubmissionManager;
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _submissionsIntService = submissionsIntService;
            _applicationFormRepository = applicationFormRepository;
            _formIntService = formIntService;
            _unitOfWorkManager = unitOfWorkManager;
        }

        public async Task<EventSubscriptionConfirmationDto> CreateIntakeSubmissionAsync(EventSubscriptionDto eventSubscriptionDto)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == eventSubscriptionDto.FormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

            JObject submissionData = await _submissionsIntService.GetSubmissionDataAsync(eventSubscriptionDto.FormId, eventSubscriptionDto.SubmissionId) ?? throw new InvalidFormDataSubmissionException();            

            // If there are no mappings on the headers then initialize the mappings
            if (applicationForm.AvailableChefsFields == null)
            {
                JToken? token = submissionData.SelectToken("submission.formVersionId");
                if (token != null)
                {
                    using var uow = _unitOfWorkManager.Begin();
                    {
                        Guid formVersionId = Guid.Parse(token.ToString());
                        var formData = await _formIntService.GetFormDataAsync(eventSubscriptionDto.FormId, formVersionId) ?? throw new InvalidFormDataSubmissionException();
                        applicationForm.AvailableChefsFields = _intakeFormSubmissionMapper.InitializeAvailableFormFields(applicationForm, formData);
                        await _applicationFormRepository.UpdateAsync(applicationForm);
                        await uow.SaveChangesAsync();
                    }
                }
            }

            var result = await _intakeFormSubmissionManager.ProcessFormSubmissionAsync(applicationForm, submissionData);
            return new EventSubscriptionConfirmationDto() { ConfirmationId = result };
        }
    }
}

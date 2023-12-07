using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Integration;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Controllers
{
    public class FormController : AbpController
    {


        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly IFormIntService _formIntService;

        public FormController(IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IFormIntService formIntService)
        {
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _applicationFormRepository = applicationFormRepository;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _formIntService = formIntService;
        }

        [HttpPost]
        [Route("/api/app/form/{formId}/version/{formVersionId}")]
        public async Task<ApplicationFormVersionDto> SynchronizeChefsAvailableFields(string formId, string formVersionId)
        {
            var applicationForm = (await _applicationFormRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsApplicationFormGuid == formId)
                    .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

            if (string.IsNullOrEmpty(applicationForm.ApiKey)) {
                throw new Exception("Application Form API Key is Required");
            }

            var chefsFormVersion = await _formIntService.GetFormDataAsync(formId, formVersionId);
            return await _applicationFormVersionAppService.UpdateOrCreateApplicationFormVersion(formId, formVersionId, applicationForm.Id, chefsFormVersion);
        }
    }
}
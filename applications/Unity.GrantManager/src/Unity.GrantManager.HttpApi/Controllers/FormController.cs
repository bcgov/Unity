using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Integration;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Controllers
{
    public class FormController : AbpController
    {

        private readonly IApplicationFormVersionRepository _applicationFormVersionRepository;
        private readonly IApplicationFormRepository _applicationFormRepository;

        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;

        private readonly IFormIntService _formIntService;

        public FormController(IIntakeFormSubmissionMapper intakeFormSubmissionMapper,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormVersionRepository applicationFormVersionRepository,
            IFormIntService formIntService)
        {
            _intakeFormSubmissionMapper = intakeFormSubmissionMapper;
            _applicationFormRepository = applicationFormRepository;
            _applicationFormVersionRepository = applicationFormVersionRepository;
            _formIntService = formIntService;
        }

        [HttpPost]
        [Route("/api/app/form/{formId}/version/{formVersionId}")]
        public async Task<ApplicationFormVersion> SynchronizeChefsAvailableFields(string formId, string formVersionId)
        {
            var applicationForm = (await _applicationFormRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsApplicationFormGuid == formId)
                    .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

            var applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsFormVersionGuid == formVersionId)
                    .FirstOrDefault();

            bool formVersionEsists = true;
            if(applicationFormVersion == null) {
                applicationFormVersion = (await _applicationFormVersionRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsApplicationFormGuid == formId && s.ChefsFormVersionGuid == null)
                    .FirstOrDefault();

                if(applicationFormVersion == null)
                {
                    applicationFormVersion = new ApplicationFormVersion();
                    applicationFormVersion.ApplicationFormId = applicationForm.Id;
                    applicationFormVersion.ChefsApplicationFormGuid = formId;
                    formVersionEsists = false;
                }

                applicationFormVersion.ChefsFormVersionGuid = formVersionId;
            }

            if (string.IsNullOrEmpty(applicationForm.ApiKey)) {
                throw new Exception("Application Form API Key is Required");
            }

            var formVersion = await _formIntService.GetFormDataAsync(Guid.Parse(formId), Guid.Parse(formVersionId));
            
            if(formVersion != null)
            {
                JToken? version = ((JObject)formVersion).SelectToken("version");
                JToken? published = ((JObject)formVersion).SelectToken("published");
                applicationFormVersion.AvailableChefsFields = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formVersion);

                if (version != null)
                {
                    applicationFormVersion.Version = int.Parse(version.ToString());
                }

                if (published != null)
                {
                    applicationFormVersion.Published = bool.Parse(published.ToString());
                }
            } else {
                throw new EntityNotFoundException("Application Form Not Registered");
            }


            if(formVersionEsists)
            {
                applicationFormVersion = await _applicationFormVersionRepository.UpdateAsync(applicationFormVersion);
            } else
            {
                applicationFormVersion = await _applicationFormVersionRepository.InsertAsync(applicationFormVersion);
            }
           
            return applicationFormVersion;
        }
    }
}
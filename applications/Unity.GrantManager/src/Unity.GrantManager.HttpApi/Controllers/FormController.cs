using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integration.Chefs;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Controllers
{
    public class FormController : AbpController
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IFormsApiService _formsApiService;

        public FormController(IApplicationFormRepository applicationFormRepository,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IFormsApiService formsApiService)
        {
            _applicationFormRepository = applicationFormRepository;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _formsApiService = formsApiService;
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
                throw new BusinessException("Application Form API Key is Required");
            }

            var chefsFormVersion = await _formsApiService.GetFormDataAsync(formId, formVersionId);
            return await _applicationFormVersionAppService.UpdateOrCreateApplicationFormVersion(formId, formVersionId, applicationForm.Id, chefsFormVersion);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Integrations.Chefs;
using Newtonsoft.Json.Linq;

namespace Unity.GrantManager.Controllers
{
    [Route("/api/app")]
    public partial class FormController(
        IApplicationFormRepository applicationFormRepository,
        IApplicationFormVersionAppService applicationFormVersionAppService,
        IFormsApiService formsApiService) : AbpController
    {
        private readonly IApplicationFormRepository _applicationFormRepository = applicationFormRepository;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService = applicationFormVersionAppService;
        private readonly IFormsApiService _formsApiService = formsApiService;

        [HttpPost("form/{formId}/version/{formVersionId}")]
        public async Task<IActionResult> SynchronizeChefsAvailableFields(string formId, string formVersionId)
        {
            // Check for model state validity (if you have a model to validate)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(formId) || string.IsNullOrWhiteSpace(formVersionId))
            {
                return BadRequest("Form ID and Version ID must be provided.");
            }

            try
            {
                var applicationForm = (await _applicationFormRepository
                    .GetQueryableAsync())
                    .Where(s => s.ChefsApplicationFormGuid == formId)
                    .FirstOrDefault() ?? throw new EntityNotFoundException("Application Form Not Registered");

                if (string.IsNullOrEmpty(applicationForm.ApiKey))
                {
                    throw new BusinessException("Application Form API Key is Required");
                }

                // Check if the application form name is current
                var updatedApplicationFormName = await SyncApplicationFormNameAsync(formId, applicationForm);

                var chefsFormVersion = await _formsApiService.GetFormDataAsync(formId, formVersionId)
                    ?? throw new BusinessException("Chefs Form Version data could not be retrieved.");

                var result = await _applicationFormVersionAppService
                    .UpdateOrCreateApplicationFormVersion(formId, formVersionId, applicationForm.Id, chefsFormVersion);

                return Ok(new { formVersion = result, updatedFormName = updatedApplicationFormName });
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (BusinessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "FormController->SynchronizeChefsAvailableFields: {ExceptionMessage}", ExceptionMessage);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }


        private async Task<string?> SyncApplicationFormNameAsync(string formId, ApplicationForm applicationForm)
        {
            var chefsForm = await _formsApiService.GetForm(
                Guid.Parse(formId),
                formId,
                applicationForm.ApiKey!) ?? throw new BusinessException("Unable to retrieve Chefs Form.");

            if (chefsForm is not JObject formObject)
                return null;

            var formName = formObject.SelectToken("name")?.ToString();
            if (string.IsNullOrWhiteSpace(formName) || formName == applicationForm.ApplicationFormName)
                return null;

            applicationForm.ApplicationFormName = formName;
            await _applicationFormRepository.UpdateAsync(applicationForm);
            return formName;
        }

    }
}
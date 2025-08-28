using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Integrations.Chefs;

namespace Unity.GrantManager.Controllers
{
    [Route("/api/app")]
    public partial class FormController(
        IApplicationFormRepository applicationFormRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicationFormVersionAppService applicationFormVersionAppService,
        IFormsApiService formsApiService) : AbpController
    {
        private readonly IApplicationFormRepository _applicationFormRepository = applicationFormRepository;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService = applicationFormVersionAppService;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
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

                var chefsFormVersion = await _formsApiService.GetFormDataAsync(formId, formVersionId)
                    ?? throw new BusinessException("Chefs Form Version data could not be retrieved.");


                var result = await _applicationFormVersionAppService
                    .UpdateOrCreateApplicationFormVersion(formId, formVersionId, applicationForm.Id, chefsFormVersion);

                return Ok(result);
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


        public class ApplicationSubmission
        {
            public string InnerHTML { set; get; } = string.Empty;
            public string SubmissionId { set; get; } = string.Empty;
        }

        [HttpPost]
        [Route("/api/app/submission")]
        [Consumes("application/json")]
        public async Task<IActionResult> StoreSubmissionHtml([FromBody] ApplicationSubmission applicationSubmission)
        {
            if (!ModelState.IsValid)
            {
                Logger.LogWarning("Invalid model state for StoreSubmissionHtml");
                return BadRequest(ModelState);
            }

            try
            {
                var applicationFormSubmission = await _applicationFormSubmissionRepository.GetAsync(Guid.Parse(applicationSubmission.SubmissionId));
                
                if (applicationFormSubmission == null)
                {
                    return NotFound("Submission not found.");
                }

                // Format HTML
                applicationFormSubmission.RenderedHTML = FormatHtml(applicationSubmission.InnerHTML);
                
                await _applicationFormSubmissionRepository.UpdateAsync(applicationFormSubmission);
                return Ok("Submission updated successfully.");
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "FormController->StoreSubmissionHtml: {ExceptionMessage}", ExceptionMessage);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        public static string FormatHtml(string html)
        {
            // Use the generated regex to replace sequences of whitespace characters with a single space
            string formattedInnerHTML = WhitespaceRegex().Replace(html, " ");
            
            // Remove new lines and tabs
            return formattedInnerHTML.Replace(Environment.NewLine, "").Replace("\t", " ");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integration.Chefs;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Controllers
{
    public partial class FormController : AbpController
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly IFormsApiService _formsApiService;

        public FormController(
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            IFormsApiService formsApiService)
        {
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
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

            if (string.IsNullOrEmpty(applicationForm.ApiKey))
            {
                throw new BusinessException("Application Form API Key is Required");
            }

            var chefsFormVersion = await _formsApiService.GetFormDataAsync(formId, formVersionId);
            return await _applicationFormVersionAppService.UpdateOrCreateApplicationFormVersion(formId, formVersionId, applicationForm.Id, chefsFormVersion);
        }


        public class ApplicationSubmission
        {
            public string InnerHTML { set; get; } = string.Empty;
            public string SubmissionId { set; get; } = string.Empty;
        }

        [HttpPost]
        [Route("/api/app/submission")]
        [Consumes("application/json")]
        public async Task StoreSubmissionHtml([FromBody] ApplicationSubmission applicationSubmission)
        {
            ApplicationFormSubmission applicationFormSubmission = await _applicationFormSubmissionRepository.GetAsync(Guid.Parse(applicationSubmission.SubmissionId));
            // Replace all double spaces
            string formattedInnerHTML = HtmlRegex().Replace(applicationSubmission.InnerHTML, " ");
            // Replace new line and tabs
            applicationFormSubmission.RenderedHTML = formattedInnerHTML.Replace(Environment.NewLine, "").Replace("\t", " ");
            await _applicationFormSubmissionRepository.UpdateAsync(applicationFormSubmission);
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex HtmlRegex();
    }
}
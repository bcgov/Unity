using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integration.Chefs;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.ApplicationForms
{

    [RemoteService(false)]
    public class ApplicationFormSycnronizationService :
    CrudAppService<
        ApplicationForm,
        ApplicationFormDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateApplicationFormDto>,
        IApplicationFormSycnronizationService
    {
        private readonly IStringEncryptionService _stringEncryptionService;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly RestClient _intakeClient;

        public List<ApplicationFormDto>? ApplicationFormDtoList { get; set; }

        public ApplicationFormSycnronizationService(IRepository<ApplicationForm,
            Guid> repository,
            RestClient restClient,
            IStringEncryptionService stringEncryptionService,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            ISubmissionAppService submissionAppService,
            IFormsApiService formsApiService)
            : base(repository)
        {
            _intakeClient = restClient;
            _stringEncryptionService = stringEncryptionService;
            _applicationFormRepository = applicationFormRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        }

        public async Task<HashSet<string>> GetMissingSubmissions()
        {

            HashSet<string> missingSubmissions = new HashSet<string>();
            // Get all forms with api keys
            ApplicationFormDtoList = (List<ApplicationFormDto>?)await GetConnectedApplicationFormsAsync();
            
            if(ApplicationFormDtoList != null)
            {
                int numberOfDaysToCheck = -100;

                foreach (ApplicationFormDto applicationFormDto in ApplicationFormDtoList)
                {
                    // Turn results into a hashet of submission ids
                    try
                    {
                        HashSet<string> newChefsSubmissions = await GetChefsSubmissions(applicationFormDto, numberOfDaysToCheck);
                        HashSet<string> existingSubmissions = (HashSet<string>)await GetSubmissionsByFormAsync(applicationFormDto.Id);
                        missingSubmissions = newChefsSubmissions.Except(existingSubmissions).ToHashSet();
                        // For each of the submissions do we have a form version to map from??
                    }
                    catch (ApiException apiException)
                    {
                        // Save the error to the application form new fields
                        // applicationFormDto.ConnectionHttpStatus = 
                    }
                }

            }

            return missingSubmissions;
        }

        public async Task<HashSet<string>> GetSubmissionsByFormAsync(Guid applicationFormId)
        {
            IQueryable<ApplicationFormSubmission> queryableApplicationFormSubmissions = _applicationFormSubmissionRepository.GetQueryableAsync().Result;
            var formSubmissionGuids = queryableApplicationFormSubmissions.Where(x => x.ApplicationFormId.Equals(applicationFormId)).Select(o => o.ChefsSubmissionGuid).ToHashSet();
            return formSubmissionGuids;
        }

        public async Task<IList<ApplicationFormDto>> GetConnectedApplicationFormsAsync()
        {
            IQueryable<ApplicationForm> queryableApplicationForms = _applicationFormRepository.GetQueryableAsync().Result;
            var forms = queryableApplicationForms.Where(x => (x.ApiKey ?? string.Empty) != string.Empty).ToList();
            return await Task.FromResult<IList<ApplicationFormDto>>(ObjectMapper.Map<List<ApplicationForm>, List<ApplicationFormDto>>(forms.ToList()));
        }

        public async Task<HashSet<string>> GetChefsSubmissions(ApplicationFormDto applicationFormDto, int numberOfDaysToCheck)
        {
            var chefsSubmissionIds = new HashSet<string>();
            string minDate = DateTime.Now.AddDays(numberOfDaysToCheck).ToString("yyyy-MM-dd");
            string maxDate = DateTime.Now.ToString("yyyy-MM-dd");
            string queryString = $"?createdAt[]={minDate}&createdAt[]={maxDate}";
            List<FormSubmissionSummaryDto>? pagedResult = await GetSubmissionsList(applicationFormDto, queryString);
            if(pagedResult != null && pagedResult.Count > 0) { 
                foreach(FormSubmissionSummaryDto submissionSummaryDto in pagedResult)
                {
                    chefsSubmissionIds.Add(submissionSummaryDto.Id.ToString());
                    // Need to store the submissionSummaryDto.FormVersionId to see if it can be mapped
                }
            }

            return chefsSubmissionIds;
        }

        public async Task<List<FormSubmissionSummaryDto>?> GetSubmissionsList(ApplicationFormDto applicationForm, string queryString)
        {
            if (applicationForm.ChefsApplicationFormGuid == null)
            {
                throw new ApiException(400, "Missing required parameter 'formId' when calling ListFormSubmissions");
            }

            string requestUrl = $"/forms/{applicationForm.ChefsApplicationFormGuid}/submissions";
            if (!string.IsNullOrEmpty(queryString))
            {
                requestUrl = requestUrl + queryString;
            }

            var restRequest = new RestRequest(requestUrl, Method.Get)
            {
                Authenticator = new HttpBasicAuthenticator(applicationForm.ChefsApplicationFormGuid!, _stringEncryptionService.Decrypt(applicationForm.ApiKey!) ?? string.Empty)
            };

            restRequest.AddParameter("deleted", "false");
            restRequest.AddParameter("filterformSubmissionStatusCode", "true");

            var response = await _intakeClient.GetAsync(restRequest);
            string errorMessageBase = "Error calling ListFormSubmissions: ";
            string errorMessage = (int)response.StatusCode switch
            {
                >= 400 => errorMessageBase + response.Content,
                0 => errorMessageBase + response.ErrorMessage,
                _ => ""
            };

            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new ApiException((int)response.StatusCode, errorMessage, response.ErrorMessage ?? $"{response.StatusCode}");
            }

            var submissionOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            List<FormSubmissionSummaryDto>? jsonResponse = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(response.Content ?? string.Empty, submissionOptions);
            return jsonResponse;
        }
    }
}

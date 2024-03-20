using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integration.Chefs;
using Unity.GrantManager.TeamsNotifications;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
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
        private readonly ICurrentTenant _currentTenant;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private readonly IApplicationFormVersionAppService _applicationFormVersionAppService;
        private readonly IConfiguration _configuration;
        private readonly ISubmissionsApiService _submissionsApiService;
        private readonly IIntakeFormSubmissionManager _intakeFormSubmissionManager;
        private List<Fact> _facts = new();
        private readonly RestClient _intakeClient;
        public List<ApplicationFormDto>? applicationFormDtoList { get; set; }
        public HashSet<string> _formVersionsInitializedVersionHash = new HashSet<string>();

        public ApplicationFormSycnronizationService(
            ICurrentTenant currentTenant,
            IRepository<ApplicationForm, Guid> repository,
            RestClient restClient,
            IConfiguration configuration,
            IStringEncryptionService stringEncryptionService,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            IApplicationFormVersionAppService applicationFormVersionAppService,
            ISubmissionsApiService submissionsApiService,
            IIntakeFormSubmissionManager intakeFormSubmissionManager)
            : base(repository)
        {
            _currentTenant = currentTenant;
            _intakeClient = restClient;
            _configuration = configuration;
            _stringEncryptionService = stringEncryptionService;
            _applicationFormRepository = applicationFormRepository;
            _submissionsApiService = submissionsApiService;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _applicationFormVersionAppService = applicationFormVersionAppService;
            _intakeFormSubmissionManager = intakeFormSubmissionManager;
        }

        private async Task SynchronizeFormSubmissions(HashSet<string> missingSubmissions, ApplicationFormDto applicationFormDto)
        {
            foreach (var submissionGuid in missingSubmissions)
            {
                // Get the formVersionId from CHEFS by submissions id -> Submission Data
                if (Guid.TryParse(applicationFormDto.ChefsApplicationFormGuid, out Guid chefsFormId) &&
                Guid.TryParse(submissionGuid, out Guid chefsSubmissionId))
                {
                    JObject? submissionData = await _submissionsApiService.GetSubmissionDataAsync(chefsFormId, chefsSubmissionId);
                    if (submissionData != null)
                    {
                        JToken? tokenFormVersionId = submissionData.SelectToken("submission.formVersionId");

                        if (tokenFormVersionId != null)
                        {
                            string formVersionId = tokenFormVersionId.ToString();
                            if (_formVersionsInitializedVersionHash.Contains(formVersionId))
                            {
                                continue; // Continue to next if the form was initialized - no point in synching data if a new form version needs creating
                            }

                            JToken? tokenVersionVersion = submissionData.SelectToken("version.version");
                            string tokenVersion = tokenVersionVersion != null ? tokenVersionVersion.ToString() : "0";

                            if (int.TryParse(tokenVersion, out int version))
                            {
                                bool formVersionExists = await _applicationFormVersionAppService.FormVersionExists(formVersionId);
                                string formId = chefsFormId.ToString();
                                if (!formVersionExists)
                                {
                                    AddFact("Form Version did NOT exist in Unity: ", "" + version);
                                    AddFact("Version Created: ", "Please Fill in Mapping");
                                    Guid.TryParse(applicationFormDto.ChefsApplicationFormGuid, out Guid applicationFormIdGuid);
                                    bool published = false;
                                    await _applicationFormVersionAppService.TryInitializeApplicationFormVersion(formId, version, applicationFormIdGuid, formVersionId, published);
                                    _formVersionsInitializedVersionHash.Add(formVersionId);
                                }
                                else
                                {
                                    ApplicationForm applicationForm = ObjectMapper.Map<ApplicationFormDto, ApplicationForm>(applicationFormDto);
                                    var result = await _intakeFormSubmissionManager.ProcessFormSubmissionAsync(applicationForm, submissionData);
                                    AddFact("Synchronizing Data - Form Version: ", version + " Unity Submission ID: " + result);
                                }
                            }

                        }
                    }
                }
            }
        }

        public async Task<HashSet<string>> GetMissingSubmissions(int numberOfDaysToCheck)
        {
            _facts = new List<Fact>();

            HashSet<string> missingSubmissions = new HashSet<string>();
            // Get all forms with api keys
            applicationFormDtoList = (List<ApplicationFormDto>?)await GetConnectedApplicationFormsAsync();

            if (applicationFormDtoList != null)
            {
                AddFact("Forms Count: ", "" + applicationFormDtoList.Count);
                foreach (ApplicationFormDto applicationFormDto in applicationFormDtoList)
                {
                    try
                    {
                        HashSet<string> newChefsSubmissions = await GetChefsSubmissions(applicationFormDto, numberOfDaysToCheck);
                        HashSet<string> existingSubmissions = GetSubmissionsByForm(applicationFormDto.Id);
                        missingSubmissions = newChefsSubmissions.Except(existingSubmissions).ToHashSet();

                        AddFact("------------------------------------", "----------------------------------------");
                        AddFact("Application Form Name: ", applicationFormDto.ApplicationFormName);
                        AddFact("Missing Submissions Count: ", missingSubmissions.Count.ToString());

                        if (missingSubmissions != null && missingSubmissions.Count > 0)
                        {
                            await SynchronizeFormSubmissions(missingSubmissions, applicationFormDto);
                        }
                    }
                    catch (HttpRequestException hrex)
                    {
                        string statusCode = hrex.StatusCode.ToString() ?? string.Empty;
                        AddFact("Application Form ApiException: ", applicationFormDto.ApplicationFormName);
                        AddFact("Status Code: ", statusCode);
                        AddFact("Message: ", hrex.Message);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Exception: {ex.Message}", ex.Message);
                    }
                }
            }

            string tenantName = "";
            if (_currentTenant != null && !string.IsNullOrEmpty(_currentTenant.Name))
            {
                tenantName = " -- Tenant: " + _currentTenant.Name;
            }

            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string activityTitle = "Review Missed Chefs Submissions " + tenantName;
            string activitySubtitle = "Environment: " + envInfo;
            string teamsChannel = _configuration["Notifications:TeamsNotificationsWebhook"] ?? "";
            await TeamsNotificationService.PostToTeamsAsync(teamsChannel, activityTitle, activitySubtitle, _facts);
            return missingSubmissions ?? new HashSet<string>();
        }

        public HashSet<string> GetSubmissionsByForm(Guid applicationFormId)
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
            if (pagedResult != null && pagedResult.Count > 0)
            {
                foreach (FormSubmissionSummaryDto submissionSummaryDto in pagedResult)
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
                Logger.LogError("Missing required parameter 'formId' when calling ListFormSubmissions");
                throw new ApiException(400, "Missing required parameter 'formId' when calling ListFormSubmissions");
            }

            string requestUrl = $"/forms/{applicationForm.ChefsApplicationFormGuid}/submissions";
            if (!string.IsNullOrEmpty(queryString))
            {
                requestUrl += queryString;
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
                Logger.LogError(errorMessage);
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

        private void AddFact(string Name, string Value)
        {
            var fact = new Fact
            {
                Name = Name,
                Value = Value
            };
            _facts.Add(fact);
        }
    }
}

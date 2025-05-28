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
using Unity.Notifications.TeamsNotifications;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integration.Chefs;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;

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
        private readonly ITenantRepository _tenantRepository;
        public List<ApplicationFormDto>? applicationFormDtoList { get; set; }
        public HashSet<string> FormVersionsInitializedVersionHash { get; set; } = new HashSet<string>();


        public ApplicationFormSycnronizationService(
            ICurrentTenant currentTenant,
            IRepository<ApplicationForm, Guid> repository,
            ITenantRepository tenantRepository,
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
            _tenantRepository = tenantRepository;
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
            try
            {
                foreach (var submissionGuid in missingSubmissions)
                {
                    await ProcessSingleSubmission(submissionGuid, applicationFormDto);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ApplicationFormSycnronizationService->SynchronizeFormSubmissions Exception: {Exception}", ex);
            }
        }

        private async Task ProcessSingleSubmission(string submissionGuid, ApplicationFormDto applicationFormDto)
        {
            if (!Guid.TryParse(applicationFormDto.ChefsApplicationFormGuid, out Guid chefsFormId) ||
                !Guid.TryParse(submissionGuid, out Guid chefsSubmissionId))
            {
                Logger.LogInformation("ApplicationFormSycnronizationService->SynchronizeFormSubmissions Invalid ChefsFormGuid or SubmissionGuid");
                return;
            }

            JObject? submissionData = await _submissionsApiService.GetSubmissionDataAsync(chefsFormId, chefsSubmissionId);
            if (submissionData == null)
            {
                Logger.LogInformation("ApplicationFormSycnronizationService->SynchronizeFormSubmissions submissionData is null");
                return;
            }

            string? formVersionId = submissionData.SelectToken("submission.formVersionId")?.ToString();
            if (formVersionId == null)
            {
                Logger.LogInformation("ApplicationFormSycnronizationService->SynchronizeFormSubmissions tokenFormVersionId is null");
                return;
            }

            if (FormVersionsInitializedVersionHash.Contains(formVersionId))
            {
                Logger.LogInformation("ApplicationFormSycnronizationService->SynchronizeFormSubmissions FormVersionsInitializedVersionHash VersionID existed {FormVersionId}", formVersionId);
                return;
            }

            var version = GetVersionFromSubmissionData(submissionData);
            if (version == -1) return;

            await ProcessFormVersion(formVersionId, version, chefsFormId, applicationFormDto, submissionData);
        }

        private int GetVersionFromSubmissionData(JObject submissionData)
        {
            JToken? tokenVersionVersion = submissionData.SelectToken("version.version");
            string tokenVersion = tokenVersionVersion?.ToString() ?? "0";

            if (!int.TryParse(tokenVersion, out int version))
            {
                Logger.LogInformation("ApplicationFormSycnronizationService->SynchronizeFormSubmissions tokenVersio -> version int not parsed");
                return -1;
            }
            return version;
        }

        private async Task ProcessFormVersion(string formVersionId, int version, Guid chefsFormId, ApplicationFormDto applicationFormDto, JObject submissionData)
        {
            bool formVersionExists = await _applicationFormVersionAppService.FormVersionExists(formVersionId);
            string formId = chefsFormId.ToString();

            if (!formVersionExists && Guid.TryParse(applicationFormDto.ChefsApplicationFormGuid, out Guid applicationFormIdGuid))
            {
                await InitializeFormVersion(formId, version, applicationFormIdGuid, formVersionId);
            }
            else
            {
                await ProcessSubmission(applicationFormDto, submissionData, version);
            }
        }

        private async Task InitializeFormVersion(string formId, int version, Guid applicationFormIdGuid, string formVersionId)
        {
            AddFact("Form Version did NOT exist in Unity: ", $"{version}");
            AddFact("Version Created: ", "Please Fill in Mapping");
            bool published = false;
            await _applicationFormVersionAppService.TryInitializeApplicationFormVersion(formId, version, applicationFormIdGuid, formVersionId, published);
            FormVersionsInitializedVersionHash.Add(formVersionId);
        }

        private async Task ProcessSubmission(ApplicationFormDto applicationFormDto, JObject submissionData, int version)
        {
            ApplicationForm applicationForm = ObjectMapper.Map<ApplicationFormDto, ApplicationForm>(applicationFormDto);
            var result = await _intakeFormSubmissionManager.ProcessFormSubmissionAsync(applicationForm, submissionData);
            AddFact("Synchronizing Data - Form Version: ", $"{version} Unity Application ID: {result}");
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
                int missingSubmissionsCount = 0;
                int formsMissingSubmissions = 0;

                foreach (ApplicationFormDto applicationFormDto in applicationFormDtoList)
                {
                    try
                    {
                        HashSet<string> newChefsSubmissions = await GetChefsSubmissions(applicationFormDto, numberOfDaysToCheck);
                        HashSet<string> existingSubmissions = GetSubmissionsByForm(applicationFormDto.Id);
                        missingSubmissions = newChefsSubmissions.Except(existingSubmissions).ToHashSet();
                        if (missingSubmissions.Count > 0)
                        {
                            formsMissingSubmissions++;
                            missingSubmissionsCount += missingSubmissions.Count;

                            AddFact("------------------------------------", "----------------------------------------");
                            AddFact("Application Form Name: ", applicationFormDto.ApplicationFormName ?? string.Empty);
                            AddFact("Missing Submissions Count: ", missingSubmissions.Count.ToString());
                            await SynchronizeFormSubmissions(missingSubmissions, applicationFormDto);
                        }
                    }
                    catch (HttpRequestException hrex)
                    {
                        string statusCode = hrex.StatusCode.ToString() ?? string.Empty;
                        AddFact("Application Form ApiException: ", applicationFormDto.ApplicationFormName ?? string.Empty);
                        AddFact("Status Code: ", statusCode);
                        AddFact("Message: ", hrex.Message);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Exception: {Exception}", ex);
                    }

                }

                AddFact("------------------------------------", "----------------------------------------");
                AddFact("Total Forms Missing Submissions: ", formsMissingSubmissions.ToString());
                AddFact("Total Missing Submissions Count: ", missingSubmissionsCount.ToString());
            }

            string tenantName = await GetTenantNameAsync() ?? "";
            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string activityTitle = "Review Missed Chefs Submissions " + tenantName;
            string activitySubtitle = "Environment: " + envInfo;
            string teamsChannel = _configuration["Notifications:TeamsNotificationsWebhook"] ?? "";
            await TeamsNotificationService.PostToTeamsAsync(teamsChannel, activityTitle, activitySubtitle, _facts);
            return missingSubmissions ?? new HashSet<string>();
        }

        private async Task<string?> GetTenantNameAsync()
        {
            string tenantName = "";
            if (_currentTenant != null && !string.IsNullOrEmpty(_currentTenant.Name))
            {
                tenantName = " -- Tenant: " + _currentTenant.Name;
            } else if (_currentTenant != null && _currentTenant.Id != null)
            {
                // Lookup the tenant name
                Tenant? tenant = await _tenantRepository.FindAsync(_currentTenant.Id.Value);
                tenantName = tenant != null ? " -- Tenant: " + tenant.Name : " -- Tenant: " + _currentTenant.Id;
            }   

            return tenantName;
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

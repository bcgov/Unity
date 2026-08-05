using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
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
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Integrations.Chefs;
using Unity.Modules.Shared.Http;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;
using Unity.GrantManager.Notifications;
using Unity.GrantManager.Notifications.Logs;

namespace Unity.GrantManager.ApplicationForms
{

    [RemoteService(false)]
    public class ApplicationFormSycnronizationService(IRepository<ApplicationForm, Guid> repository) :
    CrudAppService<
        ApplicationForm,
        ApplicationFormDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateApplicationFormDto>(repository),
        IApplicationFormSycnronizationService
    {
        private static readonly JsonSerializerOptions _submissionSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Collaborators are resolved lazily via ABP's LazyServiceProvider to keep the
        // constructor within SonarQube's 7-parameter limit (S107). CurrentTenant, Logger,
        // and ObjectMapper come from the ApplicationService base class.
        private IStringEncryptionService StringEncryptionService => LazyServiceProvider.LazyGetRequiredService<IStringEncryptionService>();
        private IApplicationFormRepository ApplicationFormRepository => LazyServiceProvider.LazyGetRequiredService<IApplicationFormRepository>();
        private IApplicationFormSubmissionRepository ApplicationFormSubmissionRepository => LazyServiceProvider.LazyGetRequiredService<IApplicationFormSubmissionRepository>();
        private IApplicationFormVersionAppService ApplicationFormVersionAppService => LazyServiceProvider.LazyGetRequiredService<IApplicationFormVersionAppService>();
        private IFormsApiService FormsApiService => LazyServiceProvider.LazyGetRequiredService<IFormsApiService>();
        private IIntakeFormSubmissionManager IntakeFormSubmissionManager => LazyServiceProvider.LazyGetRequiredService<IIntakeFormSubmissionManager>();
        private INotificationsAppService NotificationsAppService => LazyServiceProvider.LazyGetRequiredService<INotificationsAppService>();
        private IResilientHttpRequest ResilientHttpRequest => LazyServiceProvider.LazyGetRequiredService<IResilientHttpRequest>();
        private IEndpointManagementAppService EndpointManagementAppService => LazyServiceProvider.LazyGetRequiredService<IEndpointManagementAppService>();
        private ITenantRepository TenantRepository => LazyServiceProvider.LazyGetRequiredService<ITenantRepository>();

        private List<Fact> _facts = [];
        public List<ApplicationFormDto>? ApplicationFormDtoList { get; set; }
        public HashSet<string> FormVersionsInitializedVersionHash { get; set; } = [];

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
                Logger.LogError(ex, "ApplicationFormSycnronizationService->SynchronizeFormSubmissions Exception occurred");
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

            JObject? submissionData = await FormsApiService.GetSubmissionDataAsync(chefsFormId, chefsSubmissionId);
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
            bool formVersionExists = await ApplicationFormVersionAppService.FormVersionExists(formVersionId);
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
            await ApplicationFormVersionAppService.TryInitializeApplicationFormVersion(formId, version, applicationFormIdGuid, formVersionId, published);
            FormVersionsInitializedVersionHash.Add(formVersionId);
        }

        private async Task ProcessSubmission(ApplicationFormDto applicationFormDto, JObject submissionData, int version)
        {
            ApplicationForm applicationForm = ObjectMapper.Map<ApplicationFormDto, ApplicationForm>(applicationFormDto);
            var result = await IntakeFormSubmissionManager.ProcessFormSubmissionAsync(applicationForm, submissionData);
            AddFact("Synchronizing Data - Form Version: ", $"{version} Unity Application ID: {result}");
        }

        public async Task<(HashSet<string> MissingSubmissions, string MissingSubmissionsReport)> GetMissingSubmissions(int numberOfDaysToCheck)
        {
            _facts = [];
            var missingSubmissionsReportBuilder = new System.Text.StringBuilder();
            int missingSubmissionsCounter = 1;

            HashSet<string> missingSubmissions = [];
            // Get all forms with api keys
            ApplicationFormDtoList = (List<ApplicationFormDto>?) await GetConnectedApplicationFormsAsync();

            if (ApplicationFormDtoList != null)
            {
                AddFact("Forms Count: ", "" + ApplicationFormDtoList.Count);
                int missingSubmissionsCount = 0;
                int formsMissingSubmissions = 0;

                foreach (ApplicationFormDto applicationFormDto in ApplicationFormDtoList)
                {
                    try
                    {
                        HashSet<string> newChefsSubmissions = await GetChefsSubmissions(applicationFormDto, numberOfDaysToCheck);
                        HashSet<string> existingSubmissions = await GetSubmissionsByFormAsync(applicationFormDto.Id);
                        missingSubmissions = [.. newChefsSubmissions.Except(existingSubmissions)];
                        if (missingSubmissions.Count > 0)
                        {
                            formsMissingSubmissions++;
                            missingSubmissionsCount += missingSubmissions.Count;

                            AddFact("------------------------------------", "----------------------------------------");
                            AddFact("Application Form Name: ", applicationFormDto.ApplicationFormName ?? string.Empty);
                            AddFact("Missing Submissions Count: ", missingSubmissions.Count.ToString());

                            foreach (string submissionId in missingSubmissions)
                            {
                                missingSubmissionsReportBuilder.AppendLine($"{missingSubmissionsCounter}-\"{applicationFormDto.ApplicationFormName}\"-{submissionId}<br>");
                                missingSubmissionsCounter++;
                            }

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

            if (missingSubmissions.Count > 0) 
            {
                string tenantName = await GetTenantNameAsync() ?? "";
                string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                string activityTitle = "Review Missed Chefs Submissions " + tenantName;
                string activitySubtitle = "Environment: " + envInfo;
                
                await NotificationsAppService.PostToNotificationsAsync(activityTitle, activitySubtitle, _facts);
            }
            return (missingSubmissions ?? [], missingSubmissionsReportBuilder.ToString());
        }

        private async Task<string?> GetTenantNameAsync()
        {
            string tenantName = "";
            if (CurrentTenant != null && !string.IsNullOrEmpty(CurrentTenant.Name))
            {
                tenantName = " -- Tenant: " + CurrentTenant.Name;
            } else if (CurrentTenant != null && CurrentTenant.Id != null)
            {
                // Lookup the tenant name
                Tenant? tenant = await TenantRepository.FindAsync(CurrentTenant.Id.Value);
                tenantName = tenant != null ? " -- Tenant: " + tenant.Name : " -- Tenant: " + CurrentTenant.Id;
            }   

            return tenantName;
        }

        public async Task<HashSet<string>> GetSubmissionsByFormAsync(Guid applicationFormId)
        {
            IQueryable<ApplicationFormSubmission> queryableApplicationFormSubmissions = await ApplicationFormSubmissionRepository.GetQueryableAsync();
            var formSubmissionGuids = queryableApplicationFormSubmissions.Where(x => x.ApplicationFormId.Equals(applicationFormId)).Select(o => o.ChefsSubmissionGuid).ToHashSet();
            return formSubmissionGuids;
        }

        public async Task<IList<ApplicationFormDto>> GetConnectedApplicationFormsAsync()
        {
            IQueryable<ApplicationForm> queryableApplicationForms = await ApplicationFormRepository.GetQueryableAsync();
            var forms = queryableApplicationForms.Where(x => (x.ApiKey ?? string.Empty) != string.Empty).ToList();
            return ObjectMapper.Map<List<ApplicationForm>, List<ApplicationFormDto>>([.. forms]);
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

            string chefsApi = await EndpointManagementAppService.GetChefsApiBaseUrlAsync();
            string requestUrl = $"{chefsApi}/forms/{applicationForm.ChefsApplicationFormGuid}/submissions";
            if (!string.IsNullOrEmpty(queryString))
            {
                requestUrl += queryString;
            }
            requestUrl += (requestUrl.Contains('?') ? "&" : "?") + "deleted=false&filterformSubmissionStatusCode=true";

            var decryptedApiKey = StringEncryptionService.Decrypt(applicationForm.ApiKey!) ?? string.Empty;
            var response = await ResilientHttpRequest.HttpAsync(
                HttpMethod.Get,
                requestUrl,
                basicAuth: (applicationForm.ChefsApplicationFormGuid!, decryptedApiKey));

            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = "Error calling ListFormSubmissions: " + content;
                throw new ApiException((int)response.StatusCode, errorMessage, response.ReasonPhrase ?? $"{response.StatusCode}");
            }

            List<FormSubmissionSummaryDto>? jsonResponse = JsonSerializer.Deserialize<List<FormSubmissionSummaryDto>>(content, _submissionSerializerOptions);
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

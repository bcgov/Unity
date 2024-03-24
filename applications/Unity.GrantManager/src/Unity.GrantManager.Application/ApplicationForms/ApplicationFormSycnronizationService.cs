using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
using Unity.GrantManager.TeamsNotifications;
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
        private readonly IChefsMissedSubmissionsRepository _chefsMissedSubmissionsRepository;
        private readonly IConfiguration _configuration;
        private readonly RestClient _intakeClient;
        private const int PREVIOS_DAY = -1;

        public List<ApplicationFormDto>? ApplicationFormDtoList { get; set; }

        public ApplicationFormSycnronizationService(IRepository<ApplicationForm,
            Guid> repository,
            RestClient restClient,
            IConfiguration configuration,
            IStringEncryptionService stringEncryptionService,
            IApplicationFormRepository applicationFormRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
            IChefsMissedSubmissionsRepository chefsMissedSubmissionsRepository)
            : base(repository)
        {
            _intakeClient = restClient;
            _configuration = configuration;
            _stringEncryptionService = stringEncryptionService;
            _applicationFormRepository = applicationFormRepository;
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
            _chefsMissedSubmissionsRepository = chefsMissedSubmissionsRepository;
        }

        public async Task<HashSet<string>> GetMissingSubmissions()
        {
            HashSet<string> missingSubmissions = new HashSet<string>();
            // Get all forms with api keys
            ApplicationFormDtoList = (List<ApplicationFormDto>?)await GetConnectedApplicationFormsAsync();
            List<Fact> facts = new ();

            if (ApplicationFormDtoList != null)
            {
                int numberOfDaysToCheck = PREVIOS_DAY;
                Logger.LogInformation("Appform SYNC: Iterating Forms ");
                foreach (ApplicationFormDto applicationFormDto in ApplicationFormDtoList)
                {
                    try
                    {
                        HashSet<string> newChefsSubmissions = await GetChefsSubmissions(applicationFormDto, numberOfDaysToCheck);
                        HashSet<string> existingSubmissions = GetSubmissionsByForm(applicationFormDto.Id);
                        missingSubmissions = newChefsSubmissions.Except(existingSubmissions).ToHashSet();
                        Logger.LogInformation("Appform SYNC: Looking up missing: " + missingSubmissions);
                        if (missingSubmissions != null && missingSubmissions.Count > 0)
                        {
                            Logger.LogInformation("Appform SYNC: missingSubmissions: Count" + missingSubmissions.Count);
                            await _chefsMissedSubmissionsRepository.InsertAsync(
                                new ChefsMissedSubmission
                                {
                                    ChefsSubmissionGuids = string.Join(", ", missingSubmissions),
                                    ChefsApplicationFormGuid = applicationFormDto.ChefsApplicationFormGuid,
                                    TenantId = applicationFormDto.TenantId
                                }
                            );

                            // Store the count of missing submissions and Application Form Name
                            var fact = new Fact
                            {
                                Name = "Application Form Name: ",
                                Value = applicationFormDto.ApplicationFormName
                            };
                            facts.Add(fact);

                            fact = new Fact
                            {
                                Name = "Missing Submissions Count: ",
                                Value = missingSubmissions.Count.ToString()
                            };

                            facts.Add(fact);
                        }
                    }
                    catch (HttpRequestException hrex)
                    {
                        string statusCode = hrex.StatusCode.ToString() ?? string.Empty;
                        var fact = new Fact
                        {
                            Name = "Application Form ApiException: ",
                            Value = applicationFormDto.ApplicationFormName
                        };
                        facts.Add(fact);

                        fact = new Fact
                        {
                            Name = "Status Code: ",
                            Value = statusCode
                        };

                        facts.Add(fact);

                        fact = new Fact
                        {
                            Name = "Message: ",
                            Value = hrex.Message
                        };

                        facts.Add(fact);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Exception: {ex.Message}", ex.Message);
                    }
                }
            }

            string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string activityTitle = "Review Missed Chefs Submissions";
            string activitySubtitle = "Environment: " + envInfo;
            string teamsChannel = _configuration["Notifications:TeamsNotificationsWebhook"] ?? "";
            Logger.LogInformation("Getting Teams Channel: " + teamsChannel + activitySubtitle);
            await TeamsNotificationService.PostToTeamsAsync(teamsChannel, activityTitle, activitySubtitle, facts);
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
            Logger.LogInformation("ApplicationFormSynchronizationService queryString:  " + queryString);
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
            Logger.LogInformation("ApplicationFormSynchronizationService calling requestUrl:  " + requestUrl);
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
            Logger.LogInformation("ApplicationFormSynchronizationService jsonResponse:  " + jsonResponse);
            return jsonResponse;
        }
    }
}

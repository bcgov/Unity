using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integrations.Exceptions;
using Volo.Abp;
using Volo.Abp.Security.Encryption;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Unity.Modules.Shared.Http;

namespace Unity.GrantManager.Integrations.Chefs
{
    [IntegrationService] // <- registers the class as the interface it implements
    [ExposeServices(typeof(IFormsApiService))] // <- ensure it's registered under its interface
    public class FormsApiService(
            IEndpointManagementAppService endpointManagementAppService,
            IApplicationFormRepository applicationFormRepository,
            IStringEncryptionService stringEncryptionService,
            IResilientHttpRequest resilientRestClient,
            ILogger<FormsApiService> logger) : GrantManagerAppService, IFormsApiService
    {
        public async Task<JObject?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId)
        {
            var applicationForm = (await applicationFormRepository.GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == chefsFormId)
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm == null)
            {
                logger.LogWarning("No application form found for FormId: {FormId}", chefsFormId);
                return null;
            }

            string url = $"/forms/{chefsFormId}/versions/{chefsFormVersionId}";
            var response = await GetRequestAsync(url, applicationForm.ChefsApplicationFormGuid!, applicationForm.ApiKey!);

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(content);
        }

        public async Task<JObject> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey)
        {
            if (string.IsNullOrEmpty(chefsApplicationFormGuid) || string.IsNullOrEmpty(encryptedApiKey))
            {
                throw new ArgumentException("Form GUID and API Key must be provided");
            }

            string url = $"/forms/{formId}";
            var response = await GetRequestAsync(url, chefsApplicationFormGuid, encryptedApiKey);

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(content)!;
        }

        public async Task<JObject?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId)
        {
            var applicationForm = (await applicationFormRepository.GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == chefsFormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm == null)
            {
                logger.LogWarning("No application form found for SubmissionId: {SubmissionId}", submissionId);
                return null;
            }

            string url = $"/submissions/{submissionId}";
            var response = await GetRequestAsync(url, applicationForm.ChefsApplicationFormGuid!, applicationForm.ApiKey!);

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<JObject>(content);
        }

        private async Task<HttpResponseMessage> GetRequestAsync(string url, string chefsApplicationFormGuid, string encryptedApiKey)
        {
            var decryptedApiKey = stringEncryptionService.Decrypt(encryptedApiKey ?? string.Empty) ?? string.Empty;
            string chefsApi = await endpointManagementAppService.GetChefsApiBaseUrlAsync();

            resilientRestClient.SetBaseUrl(chefsApi); // Set the base URL for the API
            logger.LogInformation("Sending GET request to {Url} using basic auth", url);

            var response = await resilientRestClient.ExecuteRequestAsync(
                HttpMethod.Get,
                url,
                null,
                null,
                basicAuth: (chefsApplicationFormGuid, decryptedApiKey));

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                logger.LogError("Request to {Url} failed with status {StatusCode}. Response: {Content}", url, response.StatusCode, content);
                throw new IntegrationServiceException($"Failed to get data. Status: {response.StatusCode}, URL: {url}");
            }

            return response;
        }
    }
}

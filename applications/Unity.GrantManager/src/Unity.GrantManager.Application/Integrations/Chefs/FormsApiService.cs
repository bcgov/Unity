﻿using Newtonsoft.Json;
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
using Microsoft.EntityFrameworkCore;

namespace Unity.GrantManager.Integrations.Chefs
{
    [IntegrationService] 
    [ExposeServices(typeof(IFormsApiService))]
    public class FormsApiService : GrantManagerAppService, IFormsApiService
    {
        private readonly IEndpointManagementAppService _endpointManagementAppService;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IStringEncryptionService _stringEncryptionService;
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly ILogger<FormsApiService> _logger;

        private string? _chefsApiBaseUrl;

        public FormsApiService(
            IEndpointManagementAppService endpointManagementAppService,
            IApplicationFormRepository applicationFormRepository,
            IStringEncryptionService stringEncryptionService,
            IResilientHttpRequest resilientRestClient,
            ILogger<FormsApiService> logger)
        {
            _endpointManagementAppService = endpointManagementAppService;
            _applicationFormRepository = applicationFormRepository;
            _stringEncryptionService = stringEncryptionService;
            _resilientRestClient = resilientRestClient;
            _logger = logger;
        }

        private async Task<string> GetChefsApiBaseUrlAsync()
        {
            if (_chefsApiBaseUrl == null)
            {
                _chefsApiBaseUrl = await _endpointManagementAppService.GetChefsApiBaseUrlAsync();
            }
            return _chefsApiBaseUrl;
        }

        public async Task<JObject?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId)
        {
            var applicationForm = await (await _applicationFormRepository.GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == chefsFormId)
                .OrderBy(s => s.CreationTime)
                .FirstOrDefaultAsync();

            if (applicationForm == null)
            {
                _logger.LogWarning("No application form found for FormId: {FormId}", chefsFormId);
                return null;
            }

            string chefsApi = await GetChefsApiBaseUrlAsync();
            string url = $"{chefsApi}/forms/{chefsFormId}/versions/{chefsFormVersionId}";

            var response = await GetRequestAsync(url, applicationForm.ChefsApplicationFormGuid!, applicationForm.ApiKey!);
            return await ParseJsonResponseAsync(response);
        }

        public async Task<JObject> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey)
        {
            if (string.IsNullOrWhiteSpace(chefsApplicationFormGuid) || string.IsNullOrWhiteSpace(encryptedApiKey))
            {
                throw new ArgumentException("Form GUID and API Key must be provided");
            }

            string chefsApi = await GetChefsApiBaseUrlAsync();
            string url = $"{chefsApi}/forms/{formId}";

            var response = await GetRequestAsync(url, chefsApplicationFormGuid, encryptedApiKey);
            return await ParseJsonResponseAsync(response) ?? new JObject();
        }

        public async Task<JObject?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId)
        {
            var applicationForm = await (await _applicationFormRepository.GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == chefsFormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefaultAsync();

            if (applicationForm == null)
            {
                _logger.LogWarning("No application form found for SubmissionId: {SubmissionId}", submissionId);
                return null;
            }

            string chefsApi = await GetChefsApiBaseUrlAsync();
            string url = $"{chefsApi}/submissions/{submissionId}";

            var response = await GetRequestAsync(url, applicationForm.ChefsApplicationFormGuid!, applicationForm.ApiKey!);
            return await ParseJsonResponseAsync(response);
        }

        private async Task<HttpResponseMessage> GetRequestAsync(string url, string chefsApplicationFormGuid, string encryptedApiKey)
        {
            if (string.IsNullOrWhiteSpace(encryptedApiKey))
                throw new ArgumentException("API key is missing or empty");

            var decryptedApiKey = _stringEncryptionService.Decrypt(encryptedApiKey) ?? string.Empty;
            _logger.LogInformation(
                "Sending GET request to {Url} using basic auth with FormGuid: {FormGuid}",
                url,
                chefsApplicationFormGuid
            );

            var response = await _resilientRestClient.ExecuteRequestAsync(
                HttpMethod.Get,
                url,
                null,
                null,
                basicAuth: (chefsApplicationFormGuid, decryptedApiKey)
            );

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Request to {Url} failed with status {StatusCode} ({Reason}). Response: {Content}",
                    url,
                    response.StatusCode,
                    response.ReasonPhrase,
                    content
                );
            }

            return response;
        }

        private static async Task<JObject?> ParseJsonResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(content) ? null : JsonConvert.DeserializeObject<JObject>(content);
        }
    }
}

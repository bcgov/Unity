using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integration.Chefs;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Integrations.Http;
using Volo.Abp;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.Integrations.Chefs
{
    [IntegrationService]
    public class FormsApiService : GrantManagerAppService, IFormsApiService
    {
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IStringEncryptionService _stringEncryptionService;
        private readonly IResilientHttpRequest _resilientRestClient;

        public FormsApiService(IApplicationFormRepository applicationFormRepository,
            IStringEncryptionService stringEncryptionService,
            IResilientHttpRequest resilientRestClient)
        {
            _applicationFormRepository = applicationFormRepository;
            _stringEncryptionService = stringEncryptionService;
            _resilientRestClient = resilientRestClient;
        }

        public async Task<dynamic?> GetFormDataAsync(string chefsFormId, string chefsFormVersionId)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == chefsFormId)
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm == null) return null;

            var response = await _resilientRestClient
                .HttpAsync(Method.Get, $"/forms/{chefsFormId}/versions/{chefsFormVersionId}",
                    null,
                    null,
                    new HttpBasicAuthenticator(applicationForm.ChefsApplicationFormGuid!, _stringEncryptionService.Decrypt(applicationForm.ApiKey!) ?? string.Empty));

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }
            else
            {
                throw new IntegrationServiceException($"Error with integrating with request resource");
            }
        }

        public async Task<object> GetForm(Guid? formId, string chefsApplicationFormGuid, string encryptedApiKey)
        {
            var response = await _resilientRestClient
                .HttpAsync(Method.Get, $"/forms/{formId}",
                    null,
                    null,
                    new HttpBasicAuthenticator(chefsApplicationFormGuid!, _stringEncryptionService.Decrypt(encryptedApiKey!) ?? string.Empty));

            if (response != null
               && response.Content != null
               && response.IsSuccessStatusCode)
            {
                return response.Content;
            }
            else
            {
                throw new IntegrationServiceException($"Error with integrating with request resource");
            }
        }
    }
}


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
    public class SubmissionsApiService : GrantManagerAppService, ISubmissionsApiService
    {
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IStringEncryptionService _stringEncryptionService;

        public SubmissionsApiService(IResilientHttpRequest resilientRestClient,
            IApplicationFormRepository applicationFormRepository,
            IStringEncryptionService stringEncryptionService)
        {
            _resilientRestClient = resilientRestClient;
            _applicationFormRepository = applicationFormRepository;
            _stringEncryptionService = stringEncryptionService;
        }

        public async Task<dynamic?> GetSubmissionDataAsync(Guid chefsFormId, Guid submissionId)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == chefsFormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm == null) return null;

            var response = await _resilientRestClient
               .HttpAsync(Method.Get, $"/submissions/{submissionId}",
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
    }
}


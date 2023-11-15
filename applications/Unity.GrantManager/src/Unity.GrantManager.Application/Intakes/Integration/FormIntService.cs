using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.Intakes.Integration
{
    [RemoteService(false)]
    public class FormIntService : GrantManagerAppService, IFormIntService
    {
        private readonly RestClient _intakeClient;
        private readonly IApplicationFormRepository _applicationFormRepository;
        private readonly IStringEncryptionService _stringEncryptionService;

        public FormIntService(RestClient intakeClient,
            IApplicationFormRepository applicationFormRepository,
            IStringEncryptionService stringEncryptionService)
        {
            _intakeClient = intakeClient;
            _applicationFormRepository = applicationFormRepository;
            _stringEncryptionService = stringEncryptionService;
        }

        public async Task<dynamic?> GetFormDataAsync(Guid chefsFormId, Guid chefsFormVersionId)
        {
            var applicationForm = (await _applicationFormRepository
                .GetQueryableAsync())
                .Where(s => s.ChefsApplicationFormGuid == chefsFormId.ToString())
                .OrderBy(s => s.CreationTime)
                .FirstOrDefault();

            if (applicationForm == null) return null;

            var request = new RestRequest($"/forms/{chefsFormId}/versions/{chefsFormVersionId}")
            {
                Authenticator = new HttpBasicAuthenticator(applicationForm.ChefsApplicationFormGuid!, _stringEncryptionService.Decrypt(applicationForm.ApiKey!) ?? string.Empty)
            };

            var response = await _intakeClient.GetAsync(request);

            if (response != null 
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            return null;
        }

        public async Task<object> GetForm(Guid? formId)
        {
            var request = new RestRequest($"/forms/{formId}");
            var response = await _intakeClient.GetAsync(request);

            return response.Content ?? "Error";
        }
    }
}


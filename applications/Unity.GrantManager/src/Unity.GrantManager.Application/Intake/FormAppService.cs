using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Intake
{
    public class FormAppService : GrantManagerAppService, IFormAppService
    {
        private readonly IConfiguration _configuration;
        private readonly RestClient _intakeClient;

        public FormAppService(IConfiguration configuration, RestClient restClient)
        {
            _configuration = configuration;
            _intakeClient = restClient;
        }

        public async Task<object> GetForm(Guid? formId)
        {
            var request = new RestRequest($"/forms/{formId}");
            var response = await _intakeClient.GetAsync(request);

            // TODO: Status code response on fail
            return response.Content ?? "Error";
        }

        public async Task<List<object>> ListForms(bool? active)
        {
            // NOTE: Only allows OAUTH authentication
            throw new NotImplementedException();
        }
    }
}

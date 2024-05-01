using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Intakes
{
    [Authorize]
    public class FormAppService : GrantManagerAppService, IFormAppService
    {
        private readonly RestClient _intakeClient;

        public FormAppService(RestClient restClient)
        {
            _intakeClient = restClient;
        }

        // TODO: This needs to be mapped to a DTO
        public async Task<object> GetForm(Guid? formId)
        {
            var request = new RestRequest($"/forms/{formId}");
            var response = await _intakeClient.GetAsync(request);

            return response.Content ?? "Error";
        }

        public Task<List<object>> ListForms(bool? active)
        {
            // NOTE: Only allows OAUTH authentication
            throw new NotImplementedException();
        }
    }
}

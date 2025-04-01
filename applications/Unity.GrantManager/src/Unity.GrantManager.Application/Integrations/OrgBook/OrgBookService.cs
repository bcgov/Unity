using Newtonsoft.Json;
using RestSharp;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Orgbook;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Integrations.Http;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Integrations.Orgbook
{

    [ExposeServices(typeof(OrgBookService), typeof(IOrgBookService))]
    public class OrgBookService : ApplicationService, IOrgBookService
    {
        private readonly IResilientHttpRequest _resilientRestClient;

        private readonly string orgbook_base_api = "https://orgbook.gov.bc.ca/api";
        private readonly string orgbook_query_match = "inactive=any&latest=any&revoked=any&ordering=-score";

        public OrgBookService(IResilientHttpRequest resilientRestClient) {
            _resilientRestClient = resilientRestClient;
        }

        public async Task<dynamic?> GetOrgBookQueryAsync(string orgBookQuery)
        {
            var response = await _resilientRestClient
                .HttpAsync(Method.Get, $"{orgbook_base_api}/v4/search/topic?q={orgBookQuery}&{orgbook_query_match}");

            if (response != null && response.Content != null)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }
            else
            {
                throw new IntegrationServiceException("GetOrgBookByNumberAsync -> No Response");
            }
        }

        public async Task<JsonDocument> GetOrgBookAutocompleteQueryAsync(string? orgBookQuery)
        {
            if (orgBookQuery == null)
            {
                return JsonDocument.Parse("{}");
            }

            var response = await _resilientRestClient
                .HttpAsync(Method.Get, $"{orgbook_base_api}/v3/search/autocomplete?q={orgBookQuery}&revoked=false&inactive=");

            if (response != null && response.Content != null)
            {

                return JsonDocument.Parse(response.Content);
            }
            else
            {
                throw new IntegrationServiceException("Failed to connect to Org Book");
            }
        }

        public async Task<JsonDocument> GetOrgBookDetailsQueryAsync(string? orgBookId)
        {
            if (orgBookId == null)
            {
                return JsonDocument.Parse("{}");
            }

            var response = await _resilientRestClient
                .HttpAsync(Method.Get, $"{orgbook_base_api}/v2/topic/ident/registration.registries.ca/{orgBookId}/formatted");

            if (response != null && response.Content != null)
            {

                return JsonDocument.Parse(response.Content);
            }
            else
            {
                throw new IntegrationServiceException("Failed to connect to Org Book");
            }
        }
    }
}


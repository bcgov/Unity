using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Orgbook;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Integrations.Http;
using Volo.Abp;

namespace Unity.GrantManager.Integrations.Orgbook
{
    [IntegrationService]
    public class OrgBookService : GrantManagerAppService, IOrgBookService
    {
        private readonly IResilientHttpRequest _resilientRestClient;

        private readonly string orgbook_base_api = "https://orgbook.gov.bc.ca/api/v4";
        private readonly string orgbook_query_match = "inactive=any&latest=any&revoked=any&ordering=-score";

        public OrgBookService(IResilientHttpRequest resilientRestClient) {
            _resilientRestClient = resilientRestClient;
        }

        public async Task<dynamic?> GetOrgBookQueryAsync(string orgBookQuery)
        {
            var response = await _resilientRestClient
                .HttpAsync(Method.Get, $"{orgbook_base_api}/search/topic?q={orgBookQuery}&{orgbook_query_match}");

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
    }
}


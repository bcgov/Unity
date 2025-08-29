using Newtonsoft.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations.Exceptions;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using System.Net.Http;
using Unity.Modules.Shared.Http;
using System;

namespace Unity.GrantManager.Integrations.Orgbook
{
    [ExposeServices(typeof(OrgBookService), typeof(IOrgBookService))]
    public class OrgBookService : ApplicationService, IOrgBookService
    {
        private readonly IResilientHttpRequest resilientRestClient;
        private readonly Task<string> orgbookBaseApiTask;

        private const string OrgbookQueryMatch = "inactive=any&latest=any&revoked=any&ordering=-score";

        public OrgBookService(
            IResilientHttpRequest resilientRestClient,
            IEndpointManagementAppService endpointManagementAppService)
        {
            this.resilientRestClient = resilientRestClient;

            // Initialize the base API URL once during construction
            orgbookBaseApiTask = InitializeBaseApiAsync(endpointManagementAppService);
        }

        private static async Task<string> InitializeBaseApiAsync(IEndpointManagementAppService endpointManagementAppService)
        {
            var url = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.ORGBOOK_API_BASE);
            return url ?? throw new IntegrationServiceException("OrgBook API base URL is not configured.");
        }

        public async Task<dynamic?> GetOrgBookQueryAsync(string orgBookQuery)
        {
            var baseApi = await orgbookBaseApiTask;
            var queryParams = $"q={Uri.EscapeDataString(orgBookQuery)}&{OrgbookQueryMatch}";
            var response = await resilientRestClient.HttpAsync(
                HttpMethod.Get,
                $"{baseApi}/v4/search/topic?{queryParams}",
                null, null, null);

            if (response?.Content == null)
                throw new IntegrationServiceException("OrgBook query request returned no response.");

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(content)!;
        }

        public async Task<JsonDocument> GetOrgBookAutocompleteQueryAsync(string? orgBookQuery)
        {
            if (string.IsNullOrWhiteSpace(orgBookQuery))
                return JsonDocument.Parse("{}");

            var baseApi = await orgbookBaseApiTask;
            var url = $"{baseApi}/v3/search/autocomplete?q={Uri.EscapeDataString(orgBookQuery)}&revoked=false&inactive=";
            var response = await resilientRestClient.HttpAsync(HttpMethod.Get, url, null, null, null);

            if (response?.Content == null)
                throw new IntegrationServiceException("OrgBook autocomplete request returned no response.");

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<JsonDocument> GetOrgBookDetailsQueryAsync(string? orgBookId)
        {
            if (string.IsNullOrWhiteSpace(orgBookId))
                return JsonDocument.Parse("{}");

            var baseApi = await orgbookBaseApiTask;
            var url = $"{baseApi}/v2/topic/ident/registration.registries.ca/{Uri.EscapeDataString(orgBookId)}/formatted";
            var response = await resilientRestClient.HttpAsync(HttpMethod.Get, url, null, null, null);

            if (response?.Content == null)
                throw new IntegrationServiceException("OrgBook details request returned no response.");

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}

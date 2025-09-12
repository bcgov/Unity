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
    public class OrgBookService(
        IResilientHttpRequest resilientRestClient,
        IEndpointManagementAppService endpointManagementAppService) : ApplicationService, IOrgBookService
    {

        private readonly Task<string> _orgbookBaseApiTask = GetBaseApiUrlAsync(endpointManagementAppService);

        private const string OrgbookQueryMatch = "inactive=any&latest=any&revoked=any&ordering=-score";

        private static async Task<string> GetBaseApiUrlAsync(IEndpointManagementAppService endpointManagementAppService)
        {
            var url = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.ORGBOOK_API_BASE);
            if (string.IsNullOrEmpty(url))
            {
                throw new IntegrationServiceException("OrgBook API base URL is not configured.");
            }

            return url;
        }

        public async Task<dynamic?> GetOrgBookQueryAsync(string orgBookQuery)
        {
            if (string.IsNullOrWhiteSpace(orgBookQuery))
            {
                throw new ArgumentException("Query cannot be empty.", nameof(orgBookQuery));
            }

            var baseApi = await _orgbookBaseApiTask;
            var queryParams = $"q={Uri.EscapeDataString(orgBookQuery)}&{OrgbookQueryMatch}";
            var response = await resilientRestClient.HttpAsync(
                HttpMethod.Get,
                $"{baseApi}/v4/search/topic?{queryParams}",
                null, null, null);

            if (response?.Content == null)
            {
                throw new IntegrationServiceException("OrgBook query request returned no response.");
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(content)!;
        }

        public async Task<JsonDocument> GetOrgBookAutocompleteQueryAsync(string? orgBookQuery)
        {
            if (string.IsNullOrWhiteSpace(orgBookQuery))
            {
                return JsonDocument.Parse("{}");
            }

            var baseApi = await _orgbookBaseApiTask;
            var url = $"{baseApi}/v3/search/autocomplete?q={Uri.EscapeDataString(orgBookQuery)}&revoked=false&inactive=";
            var response = await resilientRestClient.HttpAsync(HttpMethod.Get, url, null, null, null);

            if (response?.Content == null)
            {
                throw new IntegrationServiceException("OrgBook autocomplete request returned no response.");
            }

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<JsonDocument> GetOrgBookDetailsQueryAsync(string? orgBookId)
        {
            if (string.IsNullOrWhiteSpace(orgBookId))
            {
                return JsonDocument.Parse("{}");
            }

            var response = await GetOrgBookQueryAsync(orgBookId);

            var results = response?.results as Newtonsoft.Json.Linq.JArray;
            if (results is { Count: > 0 })
            {
                var firstResultJson = JsonConvert.SerializeObject(results[0]);
                return JsonDocument.Parse(firstResultJson);
            }

            return JsonDocument.Parse("{}");
        }
    }
}

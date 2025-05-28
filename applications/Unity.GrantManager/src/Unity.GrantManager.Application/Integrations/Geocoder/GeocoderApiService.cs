using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.Modules.Shared.Http;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Integrations.Geocoder
{
    [IntegrationService]
    [ExposeServices(typeof(GeocoderApiService), typeof(IGeocoderApiService))]
    public class GeocoderApiService : ApplicationService, IGeocoderApiService
    {
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IConfiguration _configuration;

        public GeocoderApiService(
            IResilientHttpRequest resilientRestClient,
            IConfiguration configuration)
        {
            _resilientRestClient = resilientRestClient;
            _configuration = configuration;
        }

        public async Task<AddressDetailsDto> GetAddressDetailsAsync(string address)
        {
            var resource = $"{_configuration["Geocoder:LocationDetails:BaseUri"]}/addresses.json?outputSRS=3005&addressString={address}";
            var result = await GetGeoCodeDataSegmentAsync(resource);
            return ResultMapper.MapToLocation(result);
        }

        public async Task<ElectoralDistrictDto> GetElectoralDistrictAsync(LocationCoordinates locationCoordinates)
        {
            var resource = BuildGeocoderQuery(
                featureKey: "ElectoralDistrict:feature",
                propertyKey: "ElectoralDistrict:property",
                queryTypeKey: "ElectoralDistrict:querytype",
                coordinates: locationCoordinates);
            var result = await GetGeoCodeDataSegmentAsync(resource);
            return ResultMapper.MapToElectoralDistrict(result);
        }

        public async Task<EconomicRegionDto> GetEconomicRegionAsync(LocationCoordinates locationCoordinates)
        {
            var resource = BuildGeocoderQuery(
                featureKey: "EconomicRegion:feature",
                propertyKey: "EconomicRegion:property",
                queryTypeKey: "EconomicRegion:querytype",
                coordinates: locationCoordinates);
            var result = await GetGeoCodeDataSegmentAsync(resource);
            return ResultMapper.MapToEconomicRegion(result);
        }

        public async Task<RegionalDistrictDto> GetRegionalDistrictAsync(LocationCoordinates locationCoordinates)
        {
            var resource = BuildGeocoderQuery(
                featureKey: "RegionalDistrict:feature",
                propertyKey: "RegionalDistrict:property",
                queryTypeKey: "RegionalDistrict:querytype",
                coordinates: locationCoordinates);
            var result = await GetGeoCodeDataSegmentAsync(resource);
            return ResultMapper.MapToRegionalDistrict(result);
        }

        private async Task<dynamic?> GetGeoCodeDataSegmentAsync(string resource)
        {
            var response = await _resilientRestClient.ExecuteRequestAsync(HttpMethod.Get, resource, null, null);

            if (response != null && response.IsSuccessStatusCode && response.Content != null)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }
            else
            {
                throw new IntegrationServiceException($"Error integrating with resource: {resource}. Status: {response?.StatusCode}");
            }
        }

        private string BuildGeocoderQuery(string featureKey, string propertyKey, string queryTypeKey, LocationCoordinates coordinates)
        {
            var baseUri = _configuration["Geocoder:BaseUri"];
            var feature = _configuration[$"Geocoder:{featureKey}"];
            var property = _configuration[$"Geocoder:{propertyKey}"];
            var queryType = _configuration[$"Geocoder:{queryTypeKey}"];

            return $"{baseUri}{feature}" +
                   $"&srsname=EPSG:4326" +
                   $"&propertyName={property}" +
                   $"&outputFormat=application/json" +
                   $"&cql_filter=INTERSECTS({queryType},POINT({coordinates.Latitude} {coordinates.Longitude}))";
        }
    }
}

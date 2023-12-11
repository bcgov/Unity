using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Geocoder;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Integrations.Http;
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

        public GeocoderApiService(IResilientHttpRequest resilientRestClient, IConfiguration configuration)
        {
            _resilientRestClient = resilientRestClient;
            _configuration = configuration;
        }

        public async Task<AddressDetailsDto> GetAddressDetailsAsync(string address)
        {
            var resource = $"{_configuration["Geocoder:LocationDetails:BaseUri"]}/addresses.json?outputSRS=3005&addressString={address}";

            return ResultMapper.MapToLocation(await GetGeoCodeDataSegmentAsync(resource));
        }

        public async Task<ElectoralDistrictDto> GetElectoralDistrictAsync(LocationCoordinates locationCoordinates)
        {
            var resource = $"{_configuration["Geocoder:BaseUri"]}" +
                $"{_configuration["Geocoder:ElectoralDistrict:feature"]}" +
                $"&srsname=EPSG:4326" +
                $"&propertyName={_configuration["Geocoder:ElectoralDistrict:property"]}" +
                $"&outputFormat=application/json" +
                $"&cql_filter=INTERSECTS({_configuration["Geocoder:ElectoralDistrict:querytype"]}" +
                $",POINT(" + locationCoordinates.Latitude.ToString() + " " + locationCoordinates.Longitude.ToString() + "))";

            return ResultMapper.MapToElectoralDistrict(await GetGeoCodeDataSegmentAsync(resource));
        }

        public async Task<EconomicRegionDto> GetEconomicRegionAsync(LocationCoordinates locationCoordinates)
        {
            var resource = $"{_configuration["Geocoder:BaseUri"]}" +
                 $"{_configuration["Geocoder:EconomicRegion:feature"]}" +
                 $"&srsname=EPSG:4326" +
                 $"&propertyName={_configuration["Geocoder:EconomicRegion:property"]}" +
                 $"&outputFormat=application%2Fjson" +
                 $"&cql_filter=INTERSECTS({_configuration["Geocoder:EconomicRegion:querytype"]}" +
                 $",POINT(" + locationCoordinates.Latitude.ToString() + " " + locationCoordinates.Longitude.ToString() + "))";

            return ResultMapper.MapToEconomicRegion(await GetGeoCodeDataSegmentAsync(resource));
        }

        public async Task<RegionalDistrictDto> GetRegionalDistrictAsync(LocationCoordinates locationCoordinates)
        {
            var resource = $"{_configuration["Geocoder:BaseUri"]}" +
               $"{_configuration["Geocoder:RegionalDistrict:feature"]}" +
               $"&srsname=EPSG:4326" +
               $"&propertyName={_configuration["Geocoder:RegionalDistrict:property"]}" +
               $"&outputFormat=application/json" +
               $"&cql_filter=INTERSECTS({_configuration["Geocoder:RegionalDistrict:querytype"]}" +
               $",POINT(" + locationCoordinates.Latitude.ToString() + " " + locationCoordinates.Longitude.ToString() + "))";

            return ResultMapper.MapToRegionalDistrict(await GetGeoCodeDataSegmentAsync(resource));
        }

        private async Task<dynamic?> GetGeoCodeDataSegmentAsync(string resource)
        {
            var response = await _resilientRestClient.HttpAsync(Method.Get, resource);

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


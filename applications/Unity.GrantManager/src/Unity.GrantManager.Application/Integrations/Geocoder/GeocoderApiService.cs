﻿using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.Modules.Shared.Http;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Geocoder
{
    [AllowAnonymous]
    public class GeocoderApiService(IResilientHttpRequest resilientRestClient, IConfiguration configuration, IEndpointManagementAppService endpointManagementAppService) : ApplicationService
    {
        public async Task<AddressDetailsDto> GetAddressDetailsAsync(string address)
        {
            var resource = $"{configuration["Geocoder:LocationDetails:BaseUri"]}/addresses.json?outputSRS=3005&addressString={address}";
            return ResultMapper.MapToLocation(await GetGeoCodeDataSegmentAsync(resource));
        }

        public async Task<ElectoralDistrictDto> GetElectoralDistrictAsync(LocationCoordinates locationCoordinates)
        {
            var geoCoderBaseUri = await endpointManagementAppService.GetUrlByKeyNameAsync(DynamicUrlKeyNames.GEOCODER_API_BASE);
            var resource = $"{geoCoderBaseUri}" +
                $"{configuration["Geocoder:ElectoralDistrict:feature"]}" +
                $"&srsname=EPSG:4326" +
                $"&propertyName={configuration["Geocoder:ElectoralDistrict:property"]}" +
                $"&outputFormat=application/json" +
                $"&cql_filter=INTERSECTS({configuration["Geocoder:ElectoralDistrict:querytype"]}" +
                $",POINT(" + locationCoordinates.Latitude.ToString() + " " + locationCoordinates.Longitude.ToString() + "))";

            return ResultMapper.MapToElectoralDistrict(await GetGeoCodeDataSegmentAsync(resource));
        }

        public async Task<EconomicRegionDto> GetEconomicRegionAsync(LocationCoordinates locationCoordinates)
        {
            var resource = $"{configuration["Geocoder:BaseUri"]}" +
                 $"{configuration["Geocoder:EconomicRegion:feature"]}" +
                 $"&srsname=EPSG:4326" +
                 $"&propertyName={configuration["Geocoder:EconomicRegion:property"]}" +
                 $"&outputFormat=application%2Fjson" +
                 $"&cql_filter=INTERSECTS({configuration["Geocoder:EconomicRegion:querytype"]}" +
                 $",POINT(" + locationCoordinates.Latitude.ToString() + " " + locationCoordinates.Longitude.ToString() + "))";

            return ResultMapper.MapToEconomicRegion(await GetGeoCodeDataSegmentAsync(resource));
        }

        public async Task<RegionalDistrictDto> GetRegionalDistrictAsync(LocationCoordinates locationCoordinates)
        {
            var resource = $"{configuration["Geocoder:BaseUri"]}" +
               $"{configuration["Geocoder:RegionalDistrict:feature"]}" +
               $"&srsname=EPSG:4326" +
               $"&propertyName={configuration["Geocoder:RegionalDistrict:property"]}" +
               $"&outputFormat=application/json" +
               $"&cql_filter=INTERSECTS({configuration["Geocoder:RegionalDistrict:querytype"]}" +
               $",POINT(" + locationCoordinates.Latitude.ToString() + " " + locationCoordinates.Longitude.ToString() + "))";

            return ResultMapper.MapToRegionalDistrict(await GetGeoCodeDataSegmentAsync(resource));
        }

        private async Task<dynamic?> GetGeoCodeDataSegmentAsync(string resource)
        {
            var response = await resilientRestClient.HttpAsync(HttpMethod.Get, resource, null, null);

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
    }
}

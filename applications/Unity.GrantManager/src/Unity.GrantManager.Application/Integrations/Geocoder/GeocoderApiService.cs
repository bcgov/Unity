﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Geocoder;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Integrations.Http;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Geocoder
{
    //[IntegrationService]
    //[ExposeServices(typeof(GeocoderApiService), typeof(IGeocoderApiService))]
    [AllowAnonymous]
    public class GeocoderApiService(IResilientHttpRequest resilientRestClient, IConfiguration configuration) : ApplicationService, IGeocoderApiService
    {
        public async Task<AddressDetailsDto> GetAddressDetailsAsync(string address)
        {
            var resource = $"{configuration["Geocoder:LocationDetails:BaseUri"]}/addresses.json?outputSRS=3005&addressString={address}";

            return ResultMapper.MapToLocation(await GetGeoCodeDataSegmentAsync(resource));
        }

        public async Task<ElectoralDistrictDto> GetElectoralDistrictAsync(LocationCoordinates locationCoordinates)
        {
            var resource = $"{configuration["Geocoder:BaseUri"]}" +
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
            var response = await resilientRestClient.HttpAsync(Method.Get, resource);

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


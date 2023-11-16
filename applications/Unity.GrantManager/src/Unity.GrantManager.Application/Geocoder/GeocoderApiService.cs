using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Geocoder;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Geocoder
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(GeocoderApiService), typeof(IGeocoderService))]
    public class GeocoderApiService : ApplicationService, IGeocoderService
    {
        private readonly RestClient _restClient;

        public GeocoderApiService(RestClient restClient)
        {
            _restClient = restClient;
        }


        public async Task<AddressDetailsDto> GetAddressDetails(string address)
        {
            var addressDetailsDto = new AddressDetailsDto()
            {
                RequestedAddress = address
            };

            try
            {
                var locationResult = await GetLocationDetails(address);

                if (locationResult != null)
                {
                    addressDetailsDto.Location = MapToLocation(locationResult);

                    // Validate Lat and Long ?

                    if (addressDetailsDto.Location != null
                        && addressDetailsDto.Location.Coordinates != null)
                    {
                        var getElectoralDistrict = GetElectoralDistrict(addressDetailsDto.Location.Coordinates);
                        var getEconomicRegion = GetEconomicRegion(addressDetailsDto.Location.Coordinates);
                        var getRegionalDistrict = GetRegionalDistrict(addressDetailsDto.Location.Coordinates);

                        await Task.WhenAll(getElectoralDistrict, getEconomicRegion, getRegionalDistrict);

                        addressDetailsDto.ElectoralDistrict = MapToElectoralDistrict(await getElectoralDistrict);
                        addressDetailsDto.EconomicRegion = MapToEconomicRegion(await getEconomicRegion);
                        addressDetailsDto.RegionalDistrict = MapToRegionalDistrict(await getRegionalDistrict);
                    }
                }

                return addressDetailsDto;
            }
            catch (Exception)
            {
                return new AddressDetailsDto();
            }
        }

        private RegionalDistrictDto? MapToRegionalDistrict(dynamic dynamic)
        {
            return new RegionalDistrictDto();
        }

        private EconomicRegionDto? MapToEconomicRegion(dynamic dynamic)
        {
            return new EconomicRegionDto();
        }

        private ElectoralDistrictDto? MapToElectoralDistrict(dynamic dynamic)
        {
            return new ElectoralDistrictDto();
        }

        private static LocationDto? MapToLocation(dynamic locationResult)
        {
            var locationCoordinates = locationResult.features[0].geometry.coordinates;

            return new LocationDto()
            {
                Coordinates = new((double)locationCoordinates[0], (double)locationCoordinates[1])
            };
        }

        private async Task<dynamic?> GetLocationDetails(string address)
        {
            var request = new RestRequest($"https://geocoder.api.gov.bc.ca/addresses.json?outputSRS=3005&addressString={address}");

            var response = await _restClient.GetAsync(request);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            //polly library for retry on transiene errors and rate limit
            return null;
        }



        private async Task<dynamic?> GetElectoralDistrict(LocationCoordinates coordinates)
        {
            var request = new RestRequest("https://openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=pub%3AWHSE_ADMIN_BOUNDARIES.EBC_PROV_ELECTORAL_DIST_SVW&srsname=EPSG%3A4326&propertyName=ED_NAME&outputFormat=application%2Fjson&cql_filter=INTERSECTS(SHAPE%2CPOINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");

            var response = await _restClient.GetAsync(request);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            return null;
        }

        private async Task<dynamic?> GetEconomicRegion(LocationCoordinates coordinates)
        {
            var request = new RestRequest("https://openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=pub%3AWHSE_HUMAN_CULTURAL_ECONOMIC.CEN_ECONOMIC_REGIONS_SVW&srsname=EPSG%3A4326&propertyName=ECONOMIC_REGION_NAME&outputFormat=application%2Fjson&cql_filter=INTERSECTS(GEOMETRY%2CPOINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");

            var response = await _restClient.GetAsync(request);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            return null;
        }

        private async Task<dynamic?> GetRegionalDistrict(LocationCoordinates coordinates)
        {
            var request = new RestRequest("https://openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=pub%3AWHSE_LEGAL_ADMIN_BOUNDARIES.ABMS_REGIONAL_DISTRICTS_SP&srsname=EPSG%3A4326&propertyName=ADMIN_AREA_NAME&outputFormat=application%2Fjson&cql_filter=INTERSECTS(SHAPE%2CPOINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");

            var response = await _restClient.GetAsync(request);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            return null;
        }
    }
}


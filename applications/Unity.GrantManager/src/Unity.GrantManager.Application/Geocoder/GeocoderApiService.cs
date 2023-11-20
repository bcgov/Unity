using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;
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
        private readonly IConfiguration _configuration;

        public GeocoderApiService(RestClient restClient, IConfiguration configuration)
        {
            _restClient = restClient;
            _configuration = configuration;
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
            catch (Exception Ex)
            {
                return new AddressDetailsDto();
            }
        }

        private RegionalDistrictDto? MapToRegionalDistrict(dynamic dynamic)
        {
            return new RegionalDistrictDto()
            {
                Id = dynamic?.features[0]?.properties?.LGL_ADMIN_AREA_ID,
                Name = dynamic?.features[0]?.properties?.ADMIN_AREA_NAME,
            };
        }

        private EconomicRegionDto? MapToEconomicRegion(dynamic dynamic)
        {
            return new EconomicRegionDto()
            {
                Id = dynamic?.features[0]?.properties?.ECONOMIC_REGION_ID,
                Name = dynamic?.features[0]?.properties?.ECONOMIC_REGION_NAME,
            };
        }

        private ElectoralDistrictDto? MapToElectoralDistrict(dynamic dynamic)
        {
            return new ElectoralDistrictDto()
            {
                Id = dynamic?.features[0]?.properties?.ELECTORAL_DISTRICT_ID,
                Abbreviation = dynamic?.features[0]?.properties?.ED_ABBREVIATION,
                Name = dynamic?.features[0]?.properties?.ED_NAME,

            };
        }

        private static LocationDto? MapToLocation(dynamic locationResult)
        {
            var locationCoordinates = locationResult.features[0].geometry.coordinates;

            return new LocationDto()
            {
                Score = locationResult?.features[0]?.properties?.score,
                FullAddress = locationResult?.features[0]?.properties?.fullAddress,
                StreetName = locationResult?.features[0]?.properties?.streetName,
                StreetType = locationResult?.features[0]?.properties?.streetType,
                LocalityName = locationResult?.features[0]?.properties?.localityName,
                LocalityType = locationResult?.features[0]?.properties?.localityType,
                ProvinceCode = locationResult?.features[0]?.properties?.provinceCode,
                ElectoralArea = locationResult?.features[0]?.properties?.electoralArea,
                Coordinates = new((double)locationCoordinates[0], (double)locationCoordinates[1])
            };
        }

        private async Task<dynamic?> GetLocationDetails(string address)
        {
            try
            {

                var retryPolicy = Policy
               .Handle<Exception>() // Customize this based on your API's exception types
               .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),onRetry: (exception, retryAttempt) =>
               {
                   Console.WriteLine("Error: " + exception.Message + "... Retry Count " + retryAttempt);
               });


                var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));


                var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);


                var result = await policyWrap.ExecuteAsync<dynamic?>(async () => {
                    var request = new RestRequest($"{_configuration["Geocoder:LocationDetails:BaseUri"]}/addresses.json?outputSRS=3005&addressString={address}");

                    var response = await _restClient.GetAsync(request);

                    if (response != null
                        && response.Content != null
                        && response.IsSuccessStatusCode)
                    {
                        string content = response.Content;
                        return JsonConvert.DeserializeObject<dynamic>(content)!;
                    }
                    throw new Exception(response.ErrorMessage);
                });
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }




        private async Task<dynamic?> GetElectoralDistrict(LocationCoordinates coordinates)
        {
            try
            {
                var retryPolicy = Policy
              .Handle<Exception>() // Customize this based on your API's exception types
              .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (exception, retryAttempt) =>
              {
                  Console.WriteLine("Error: " + exception.Message + "... Retry Count " + retryAttempt);
              });


                var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));


                var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
                var result = await policyWrap.ExecuteAsync<dynamic?>(async () => { 
                    var request = new RestRequest($"{_configuration["Geocoder:BaseUri"]}" +
                $"{_configuration["Geocoder:ElectoralDistrict:feature"]}" +
                $"&srsname=EPSG:4326" +
                $"&propertyName={_configuration["Geocoder:ElectoralDistrict:property"]}" +
                $"&outputFormat=application/json" +
                $"&cql_filter=INTERSECTS({_configuration["Geocoder:ElectoralDistrict:querytype"]}" +
                $",POINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");

                var response = await _restClient.GetAsync(request);

                if (response != null
                    && response.Content != null
                    && response.IsSuccessStatusCode)
                {
                    string content = response.Content;
                    return JsonConvert.DeserializeObject<dynamic>(content)!;
                }

                return new Exception(response.ErrorMessage);
                });
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<dynamic?> GetEconomicRegion(LocationCoordinates coordinates)
        {
            try
            {

                var retryPolicy = Policy
                .Handle<Exception>() // Customize this based on your API's exception types
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (exception, retryAttempt) =>
                {
                    Console.WriteLine("Error: " + exception.Message + "... Retry Count " + retryAttempt);
                });


                var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));
                var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
                var result = await policyWrap.ExecuteAsync<dynamic?>(async () =>
                {

                    var request = new RestRequest($"{_configuration["Geocoder:BaseUri"]}" +
                 $"{_configuration["Geocoder:EconomicRegion:feature"]}" +
                 $"&srsname=EPSG:4326" +
                 $"&propertyName={_configuration["Geocoder:EconomicRegion:property"]}" +
                 $"&outputFormat=application%2Fjson" +
                 $"&cql_filter=INTERSECTS({_configuration["Geocoder:EconomicRegion:querytype"]}" +
                 $",POINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");


                    var response = await _restClient.GetAsync(request);

                    if (response != null
                        && response.Content != null
                        && response.IsSuccessStatusCode)
                    {
                        string content = response.Content;
                        return JsonConvert.DeserializeObject<dynamic>(content)!;
                    }

                 return new Exception(response.ErrorMessage);
                });
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<dynamic?> GetRegionalDistrict(LocationCoordinates coordinates)
        {
            var retryPolicy = Policy
               .Handle<Exception>() // Customize this based on your API's exception types
               .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (exception, retryAttempt) =>
               {
                   Console.WriteLine("Error: " + exception.Message + "... Retry Count " + retryAttempt);
               });


            var circuitBreakerPolicy = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));
            var policyWrap = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
            var result = await policyWrap.ExecuteAsync<dynamic?>(async () =>
            {

                var request = new RestRequest($"{_configuration["Geocoder:BaseUri"]}" +
               $"{_configuration["Geocoder:RegionalDistrict:feature"]}" +
               $"&srsname=EPSG:4326" +
               $"&propertyName={_configuration["Geocoder:RegionalDistrict:property"]}" +
               $"&outputFormat=application/json" +
               $"&cql_filter=INTERSECTS({_configuration["Geocoder:RegionalDistrict:querytype"]}" +
               $",POINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");
                var response = await _restClient.GetAsync(request);

                if (response != null
                    && response.Content != null
                    && response.IsSuccessStatusCode)
                {
                    string content = response.Content;
                    return JsonConvert.DeserializeObject<dynamic>(content)!;
                }

                return new Exception(response.ErrorMessage); ;
            });
            return result;
        }
    }
}


using Amazon.Runtime;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Geocoder;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Geocoader
{
    public class Coordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }


    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(GeocoderApiService), typeof(IGeocoaderService))]
    public class GeocoderApiService : ApplicationService, IGeocoaderService
    {
        private readonly RestClient _intakeClient;

        public GeocoderApiService(RestClient intakeClient)
        {
            _intakeClient = intakeClient;
        }


        public async Task<dynamic?> GetAddressDetails(string value)
        {
            try
            {
                var locationResult = await GetLocationDetails(value);

                if (locationResult != null)
                {
                    var locationCoordinates = locationResult.features[0].geometry.coordinates;

                    var coordinates = new Coordinates();
                    coordinates.Latitude = locationCoordinates[0];
                    coordinates.Longitude = locationCoordinates[1];

                    var getElectoralDistrict = GetElectoralDistrict(coordinates);
                    var getEconomicRegion = GetEconomicRegion(coordinates);
                    var getRegionalDistrict = GetRegionalDistrict(coordinates);

                    await Task.WhenAll(getElectoralDistrict, getEconomicRegion, getRegionalDistrict);


                    var combinedResult = new
                    {
                        electoralDistrict = await getElectoralDistrict,
                        economicRegion = await getEconomicRegion,
                        regionalDistrict = await getRegionalDistrict,
                        location = locationResult
                    };

                    dynamic data = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(combinedResult));
                    //dynamic data = JsonConvert.SerializeObject(combinedResult);
                    return data;


                }
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }


        }


        private async Task<dynamic?> GetLocationDetails(string address)
        {
            var request = new RestRequest($"https://geocoder.api.gov.bc.ca/addresses.json?outputSRS=3005&addressString={address}");

            var response = await _intakeClient.GetAsync(request);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            return null;
        }



        private async Task<dynamic?> GetElectoralDistrict(Coordinates coordinates)
        {
            var request = new RestRequest("https://openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=pub%3AWHSE_ADMIN_BOUNDARIES.EBC_PROV_ELECTORAL_DIST_SVW&srsname=EPSG%3A4326&propertyName=ED_NAME&outputFormat=application%2Fjson&cql_filter=INTERSECTS(SHAPE%2CPOINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");

            var response = await _intakeClient.GetAsync(request);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            return null;
        }

        private async Task<dynamic?> GetEconomicRegion(Coordinates coordinates)
        {
            var request = new RestRequest("https://openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=pub%3AWHSE_HUMAN_CULTURAL_ECONOMIC.CEN_ECONOMIC_REGIONS_SVW&srsname=EPSG%3A4326&propertyName=ECONOMIC_REGION_NAME&outputFormat=application%2Fjson&cql_filter=INTERSECTS(GEOMETRY%2CPOINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");

            var response = await _intakeClient.GetAsync(request);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                return JsonConvert.DeserializeObject<dynamic>(content)!;
            }

            return null;
        }

        private async Task<dynamic?> GetRegionalDistrict(Coordinates coordinates)
        {
            var request = new RestRequest("https://openmaps.gov.bc.ca/geo/pub/ows?service=WFS&version=1.0.0&request=GetFeature&typeName=pub%3AWHSE_LEGAL_ADMIN_BOUNDARIES.ABMS_REGIONAL_DISTRICTS_SP&srsname=EPSG%3A4326&propertyName=ADMIN_AREA_NAME&outputFormat=application%2Fjson&cql_filter=INTERSECTS(SHAPE%2CPOINT(" + coordinates.Latitude.ToString() + " " + coordinates.Longitude.ToString() + "))");

            var response = await _intakeClient.GetAsync(request);

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


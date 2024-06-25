using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Worksheets.Values;
using Unity.GrantManager.Integration.Geocoder;

namespace Unity.Flex.Worksheets.Collectors
{
    public class BCAddressCollector : IDataCollector<CustomValueBase, BCAddressValue>
    {
        private readonly IServiceProvider _serviceProvider;

        public BCAddressCollector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<BCAddressValue> CollectAsync(CustomValueBase value)
        {
            var geoService = _serviceProvider.GetRequiredService<IGeocoderApiService>();

            if (value.Value == null) return (BCAddressValue)value;

            var BCAddress = JsonSerializer.Deserialize<BCAddressLocationValue>(value.Value?.ToString() ?? "{}");

            if (BCAddress == null
                || BCAddress.Properties == null
                || BCAddress.Properties.FullAddress == null
                || BCAddress.Geometry == null
                || BCAddress.Geometry.Coordinates == null)
            {
                return (BCAddressValue)value;
            }

            try
            {
                var addressDetails = await geoService.GetAddressDetailsAsync(BCAddress.Properties.FullAddress);

                if (addressDetails == null || addressDetails.Coordinates == null) return (BCAddressValue)value;

                var coords = new LocationCoordinates(addressDetails.Coordinates.Latitude, addressDetails.Coordinates.Longitude);

                var electoralDistrict = await geoService.GetElectoralDistrictAsync(coords);
                var regionalDistrict = await geoService.GetRegionalDistrictAsync(coords);
                var economicRegion = await geoService.GetEconomicRegionAsync(coords);

                BCAddress.GeoElectoralDistrict = $"{electoralDistrict.Id}-({electoralDistrict.Abbreviation}) {electoralDistrict.Name}";
                BCAddress.GeoRegionalDistrict = $"{regionalDistrict.Id}-{regionalDistrict.Name}";
                BCAddress.GeoEconomicRegion = $"{regionalDistrict.Id}-{economicRegion.Name}";
            }
            catch (Exception ex)
            {
                // Need improve the error handling for the geo coder services on the geocoder side - safety net for now                
                Debug.WriteLine(ex.Message);
            }

            return new BCAddressValue(BCAddress);
        }
    }
}

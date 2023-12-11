using Unity.GrantManager.Integration.Geocoder;

namespace Unity.GrantManager.Integrations.Geocoder
{
    internal static class ResultMapper
    {
        internal static RegionalDistrictDto? MapToRegionalDistrict(dynamic dynamic)
        {
            return new RegionalDistrictDto()
            {
                Id = dynamic?.features[0]?.properties?.LGL_ADMIN_AREA_ID,
                Name = dynamic?.features[0]?.properties?.ADMIN_AREA_NAME,
            };
        }

        internal static EconomicRegionDto? MapToEconomicRegion(dynamic dynamic)
        {
            return new EconomicRegionDto()
            {
                Id = dynamic?.features[0]?.properties?.ECONOMIC_REGION_ID,
                Name = dynamic?.features[0]?.properties?.ECONOMIC_REGION_NAME,
            };
        }

        internal static ElectoralDistrictDto? MapToElectoralDistrict(dynamic dynamic)
        {
            return new ElectoralDistrictDto()
            {
                Id = dynamic?.features[0]?.properties?.ELECTORAL_DISTRICT_ID,
                Abbreviation = dynamic?.features[0]?.properties?.ED_ABBREVIATION,
                Name = dynamic?.features[0]?.properties?.ED_NAME,
            };
        }

        internal static AddressDetailsDto? MapToLocation(dynamic locationResult)
        {
            var locationCoordinates = locationResult.features[0].geometry.coordinates;

            return new AddressDetailsDto()
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
    }
}

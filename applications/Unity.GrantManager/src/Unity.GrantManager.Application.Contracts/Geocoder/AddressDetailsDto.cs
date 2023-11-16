namespace Unity.GrantManager.Geocoder
{
    public class AddressDetailsDto
    {
        public string RequestedAddress { get; set; } = string.Empty;
        public ElectoralDistrictDto? ElectoralDistrict { get;set; }
        public EconomicRegionDto? EconomicRegion { get; set; }
        public RegionalDistrictDto? RegionalDistrict { get; set; }
        public LocationDto? Location { get; set; }
    }

    public class ElectoralDistrictDto
    {

    }

    public class EconomicRegionDto
    {

    }

    public class RegionalDistrictDto
    {

    }

    public class LocationDto
    {
        public LocationCoordinates? Coordinates { get; set; }
    }

    public class LocationCoordinates
    {
        public LocationCoordinates()
        {
        }
        public LocationCoordinates(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
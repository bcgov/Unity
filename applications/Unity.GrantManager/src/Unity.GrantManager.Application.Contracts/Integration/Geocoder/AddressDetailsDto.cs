namespace Unity.GrantManager.Integration.Geocoder
{
    public class AddressDetailsDto
    {
        public LocationCoordinates? Coordinates { get; set; }

        public int? Score { get; set; }
        public string? FullAddress { get; set; }
        public string? StreetName { get; set; }
        public string? StreetType { get; set; }
        public string? LocalityName { get; set; }
        public string? LocalityType { get; set; }
        public string? ProvinceCode { get; set; }
        public string? ElectoralArea { get; set; }
    }

    public class ElectoralDistrictDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Abbreviation { get; set; }

    }

    public class EconomicRegionDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
    }

    public class RegionalDistrictDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
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
using System.Collections.Generic;

namespace Unity.Flex.Worksheets.Values
{
    public class BCAddressLocationValue
    {     
        public string? Type { get; set; } = null;     
        public Geometry? Geometry { get; set; }        
        public LocationProperties? Properties { get; set; }

        public string? GeoElectoralDistrict { get; set; }
        public string? GeoRegionalDistrict { get; set; }
        public string? GeoEconomicRegion { get; set; }
    }

    public class Geometry
    {             
        public string? Type { get; set; } = null;        
        public List<double>? Coordinates { get; set; }
    }

    public class LocationProperties
    {        
        public int? Score { get; set; }     
        public string? FullAddress { get; set; }
        public string? LocalityType { get; set; }
        public string? ElectoralArea { get; set; }
        public string? MatchPrecision { get; set; }
        public int? PrecisionPoints { get; set; }
        public string? LocationPositionalAccuracy { get; set; }        
    }    
}

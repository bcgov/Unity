using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Unity.Flex.Worksheets.Values;

namespace Unity.GrantManager.Intakes.Transformers
{
    public class BCAddressTransformer : IValueTransformer<JToken, BCAddressValue>
    {
        public BCAddressValue Transform(JToken value)
        {
            JObject obj = JObject.FromObject(value);
            var typeNode = obj.GetValue("type");
            var geometryNode = obj.GetValue("geometry");
            var propertiesNode = obj.GetValue("properties");
            BCAddressLocationValue bcAddressValue = new()
            {
                Type = typeNode?.ToString()
            };

            BuildGeometry(bcAddressValue, geometryNode);
            BuildLocationProperties(bcAddressValue, propertiesNode);

            return new BCAddressValue(bcAddressValue);
        }

        private static void BuildGeometry(BCAddressLocationValue bcAddressValue, JToken? geometryNode)
        {
            if (geometryNode == null) return;
            var obj = JObject.FromObject(geometryNode);

            var geometry = new Geometry
            {
                Type = obj.GetValue("type")?.ToString(),
                Coordinates = JsonSerializer.Deserialize<List<double>>(obj.GetValue("coordinates")?.ToString() ?? "[]")
            };

            bcAddressValue.Geometry = geometry;
        }

        private static void BuildLocationProperties(BCAddressLocationValue bcAddressValue, JToken? propertiesNode)
        {
            if (propertiesNode == null) return;

            var locationProperties = new LocationProperties();

            var obj = JObject.FromObject(propertiesNode);

            locationProperties.FullAddress = obj.GetValue("fullAddress")?.ToString();
            locationProperties.Score = int.Parse(obj.GetValue("score")?.ToString() ?? "0");

            bcAddressValue.Properties = locationProperties;
        }
    }
}

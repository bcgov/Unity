using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GrantManager.Intakes
{
    public static class FileKeyFinder
    {

        public static List<string> FindFileKeys(JToken json, string key, string value)
        {
            var nodes = new List<JToken>();
            FindFileKeyNodes(json, key, value, nodes);
            return nodes.Select(s => s.ToString()).ToList();
        }

        private static void FindFileKeyNodes(JToken json, string key, string value, List<JToken> nodes)
        {
            if (json.Type == JTokenType.Object)
            {
                foreach (JProperty child in json.Children<JProperty>())
                {
                    if (child.Name.StartsWith(key) && child.Value.ToString().Equals(value) && json!["key"] != null)
                    {
                        JToken? node = json!["key"];
                        if (node != null)
                        {
                            nodes.Add(node);
                        }
                    }
                    FindFileKeyNodes(child.Value, key, value, nodes);
                }
            }
            else if (json.Type == JTokenType.Array)
            {
                foreach (JToken child in json.Children())
                {
                    FindFileKeyNodes(child, key, value, nodes);
                }
            }
        }
    }
}
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Services;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Intakes
{
    public class InputComponentProcessor : DomainService
    {
        protected readonly Dictionary<string, string> _components = new Dictionary<string, string>();
        private static ILogger _logger = NullLogger.Instance;

        // Method to initialize the logger (if needed)
        public static void InitializeLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(InputComponentProcessor));
        }

        private static readonly HashSet<string> _allowableContainerTypes = new HashSet<string>
        {
            "tabs", "table", "simplecols2", "simplecols3", "simplecols4", 
            "simplecontent", "simplepanel", "simpleparagraph", "simpletabs", 
            "container", "columns", "panel"
        };

        private static readonly HashSet<string> _columnTypes = new HashSet<string>
        {
            "simplecols2", "simplecols3", "simplecols4", "columns"
        };

        private static readonly HashSet<string> _dynamicTypes = new HashSet<string>
        {
            "datagrid"
        };

        private void AddComponentToDictionary(string key, string? tokenType, string label)
        {
            if (key != null && !_components.ContainsKey(key))
            {
                var jsonValue = JsonConvert.SerializeObject(new { type = tokenType, label });
                _components[key] = jsonValue;
            }
        }

        // Centralized check for valid tokens, including 'input' and 'type' validation
        private static bool IsValidToken(JToken childToken)
        {
            var tokenInput = childToken["input"]?.ToString();
            var tokenType = childToken["type"]?.ToString();
            return tokenInput == "True" && tokenType != null && tokenType != "button" && !_allowableContainerTypes.Contains(tokenType);
        }

        // Determine the sub-lookup type based on the token type
        public static string GetSubLookupType(string? tokenType)
        {
            if (string.IsNullOrEmpty(tokenType))
            {
                return "components";
            }

            if (_columnTypes.Contains(tokenType))
            {
                return "columns";
            }

            if (tokenType.Equals("table", StringComparison.OrdinalIgnoreCase))
            {
                return "rows";
            }

            return "components";
        }

        // Process individual components (adding them to the dictionary and handling nested components)
        public void ProcessComponent(JToken token)
        {
            try
            {
                if (token.Type == JTokenType.Array)
                {
                    TraverseComponents(token); // Recurse into arrays
                }

                if (IsValidToken(token))
                {
                    // Safely retrieve key, label, and tokenType, and add to dictionary
                    var key = token["key"]?.ToString();
                    var label = token["label"]?.ToString();
                    var tokenType = token["type"]?.ToString();

                    // Only add to dictionary if key and label are both valid
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(label))
                    {
                        AddComponentToDictionary(key, tokenType, label);
                    }
                }

                // Process nested components, if any
                ProcessNestedComponents(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the token.");
            }
        }

        // Process nested components (dynamically or statically, depending on the type)
        private void ProcessNestedComponents(JToken token)
        {
            var subTokenType = token["type"]?.ToString();
            var subLookupType = GetSubLookupType(subTokenType);

            // Handle dynamic types (e.g., datagrid)
            if (!string.IsNullOrEmpty(subTokenType) && _dynamicTypes.Contains(subTokenType))
            {
                // Safely retrieve key and label, ensuring they're valid before calling AddComponentToDictionary
                var key = token["key"]?.ToString();
                var label = token["label"]?.ToString();

                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(label))
                {
                    AddComponentToDictionary(key, subTokenType, label);
                }
            }

            // Process any nested components under the current token
            var nestedComponents = token.SelectToken(subLookupType);
            if (nestedComponents != null)
            {
                foreach (var nestedToken in nestedComponents.Children<JToken>())
                {
                    ProcessComponent(nestedToken);
                }
            }
        }

        // Traverse through the components and process each one
        public void TraverseComponents(JToken? tokenComponents)
        {
            if (tokenComponents == null) return;

            foreach (var childToken in tokenComponents.Children<JToken>())
            {
                // Process only valid objects (avoid unnecessary recursion)
                if (childToken.Type == JTokenType.Object)
                {
                    ProcessComponent(childToken);
                }
                else if (childToken.Type == JTokenType.Array)
                {
                    TraverseComponents(childToken); // Recurse into arrays
                }
            }
        }

        // Find all nodes by name, useful for dynamic or repeated structures
        public static List<JToken> FindNodesByName(JToken json, string name)
        {
            var nodes = new List<JToken>();
            FindNodesRecursive(json, name, nodes);
            return nodes;
        }

        // Recursively search for nodes based on the given name
        protected static void FindNodesRecursive(JToken json, string name, List<JToken> nodes)
        {
            if (json.Type == JTokenType.Object)
            {
                foreach (JProperty child in json.Children<JProperty>())
                {
                    // Add nodes that match the name prefix
                    if (child.Name.StartsWith(name))
                    {
                        nodes.Add(child.Value);
                    }
                    // Continue recursion for nested children
                    FindNodesRecursive(child.Value, name, nodes);
                }
            }
            else if (json.Type == JTokenType.Array)
            {
                foreach (var child in json.Children())
                {
                    FindNodesRecursive(child, name, nodes); // Continue recursion for array elements
                }
            }
        }
    }
}

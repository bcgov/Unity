using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Services;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;

namespace Unity.GrantManager.Intakes
{
    public class InputComponentProcessor : DomainService
    {
        protected readonly Dictionary<string, string> components = new Dictionary<string, string>();
        private static ILogger logger = NullLogger.Instance;

        // Method to initialize the logger (if needed)
        public static void InitializeLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<InputComponentProcessor>();
        }

        private static readonly HashSet<string> allowableContainerTypes = new HashSet<string>
        {
            "tabs", "table", "simplecols2", "simplecols3", "simplecols4",
            "simplecontent", "simplepanel", "simpleparagraph", "simpletabs",
            "container", "columns", "panel"
        };
        private static readonly HashSet<string> columnTypes = new HashSet<string>
        {
            "simplecols2",
            "simplecols3",
            "simplecols4",
            "columns"
        };

        private static readonly HashSet<string> dynamicTypes = new HashSet<string>
        {
            "datagrid"
        };

        private static readonly HashSet<string> nestedKeyFields = new HashSet<string>
        {
            "simplecheckboxes","simplecheckboxadvanced"
        };

        private void ProcessComponentToDictionary(string key, string? tokenType, string label, string? tokenValues)
        {
            if (!components.ContainsKey(key))
            {
                var jsonValue = JsonConvert.SerializeObject(new { type = tokenType, label, values = tokenValues });
                components.Add(key, jsonValue);
            }
        }

        private static bool IsValidToken(JToken token)
        {
            var tokenInput = token["input"]?.ToString();
            var tokenType = token["type"]?.ToString();

            return tokenInput == "True" &&
                   tokenType != null &&
                   tokenType != "button" &&
                   !allowableContainerTypes.Contains(tokenType);
        }

        // Determine the sub-lookup type based on the token type
        public static string GetSubLookupType(string? tokenType)
        {
            // Default to "components" if tokenType is null or empty
            if (string.IsNullOrEmpty(tokenType))
            {
                return "components";
            }

            // Check if tokenType is part of ColumnTypes
            if (columnTypes.Contains(tokenType))
            {
                return "columns";
            }

            // Check for "table" case-insensitively
            if (tokenType.Equals("table", StringComparison.OrdinalIgnoreCase))
            {
                return "rows";
            }

            // Default return for any other tokenType
            return "components";
        }

        public void ProcessComponent(JToken token)
        {
            try
            {
                if (!IsValidToken(token)) return;

                string? key = token["key"]?.ToString();
                string? label = token["label"]?.ToString();
                string? tokenType = token["type"]?.ToString();
                string? tokenValues;

                // Check if tokenType is in nestedKeyFields and extract values
                if (key != null
                    && tokenType != null
                    && nestedKeyFields.Contains(tokenType)
                    && token["values"] is JArray valuesArray)
                {
                    tokenValues = string.Join(",", valuesArray.Select(v => v["value"]?.ToString()));
                }
                else
                {
                    tokenValues = token["values"]?.ToString();
                }


                if (key != null && label != null)
                {
                    ProcessComponentToDictionary(key, tokenType, label, tokenValues);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "An exception occurred in {MethodName}: {ExceptionMessage}", nameof(ProcessComponent), ex.Message);
            }
        }

        protected void ProcessNestedComponents(JToken? token)
        {
            if (token != null)
            {
                var subTokenType = token["type"]?.ToString();

                string subLookupType = GetSubLookupType(subTokenType);

                // Any dynamic types, get the parent and children tokens
                if (!string.IsNullOrEmpty(subTokenType) && dynamicTypes.Contains(subTokenType))
                {
                    ProcessComponent(token);
                }

                var nestedComponentsComponents = ((JObject)token).SelectToken(subLookupType);
                if (nestedComponentsComponents != null)
                {
                    TraverseComponents(nestedComponentsComponents);
                }
                else
                {
                    ProcessComponent(token);
                }
            }
        }

        // Traverse through the components and process each one
        public void TraverseComponents(JToken? tokenComponents)
        {
            if (tokenComponents == null) return;

            foreach (var childToken in tokenComponents.Children<JToken>())
            {
                if (childToken.Type == JTokenType.Array)
                {
                    TraverseComponents(childToken);
                }
                if (childToken.Type != JTokenType.Object) continue;

                ProcessChildToken(childToken);
            }
        }

        private void ProcessChildToken(JToken childToken)
        {
            var tokenType = childToken["type"];

            // Add the component if applicable
            ProcessComponent(childToken);

            if (tokenType != null
                && allowableContainerTypes.Contains(tokenType.ToString())
                && !dynamicTypes.Contains(tokenType.ToString()))
            {
                ProcessNestedComponents(childToken, tokenType);
            }
            else if (tokenType != null && dynamicTypes.Contains(tokenType.ToString()))
            {
                ProcessNestedComponents(childToken);
            }
            else if (childToken.Children().Any())
            {
                ProcessMultiNested(childToken);
            }
        }

        private void ProcessMultiNested(JToken childToken)
        {
            foreach (JProperty grandChildToken in childToken.Children<JProperty>())
            {
                if (grandChildToken.Name == "components")
                {
                    TraverseComponents(grandChildToken);
                }
            }
        }

        private void ProcessNestedComponents(JToken childToken, JToken? tokenType)
        {
            // Get the sub-token string using a safe conversion of tokenType
            var subTokenString = GetSubLookupType(tokenType?.ToString());

            // Safely select nested components
            var nestedComponents = childToken.SelectToken(subTokenString);

            // If there are nested components, process them            
            if (nestedComponents != null)
            {
                foreach (var nestedTokenComponent in nestedComponents.Children())
                {
                    ProcessNestedTokenComponent(nestedTokenComponent, subTokenString);
                }
            }
        }


        private void ProcessNestedTokenComponent(JToken nestedTokenComponent, string subTokenString)
        {
            if (subTokenString == "rows")
            {
                TraverseComponents(nestedTokenComponent);
            }
            else
            {
                ProcessNestedComponents(nestedTokenComponent);
            }
        }

        private static void FindNodesRecursive(JToken json, string name, List<JToken> nodes)
        {
            if (json.Type == JTokenType.Object)
            {
                foreach (JProperty child in json.Children<JProperty>())
                {
                    if (child.Name.StartsWith(name))
                    {
                        nodes.Add(child);
                    }
                    // Continue recursion for nested children
                    FindNodesRecursive(child.Value, name, nodes);
                }
            }
            else if (json.Type == JTokenType.Array)
            {
                foreach (JToken child in json.Children())
                {
                    FindNodesRecursive(child, name, nodes);
                }
            }
        }

        public static List<JToken> FindNodes(JToken json, string name)
        {
            var nodes = new List<JToken>();
            FindNodesRecursive(json, name, nodes);
            return nodes;
        }

    }
}

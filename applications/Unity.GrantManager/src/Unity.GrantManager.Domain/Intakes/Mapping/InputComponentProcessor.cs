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
        protected static readonly Dictionary<string, string> components = new Dictionary<string, string>();
        private static ILogger logger = NullLogger.Instance;

        // Method to initialize the logger (if needed)
        public static void InitializeLogger(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger(typeof(InputComponentProcessor));
        }

        private static readonly List<string> AllowableContainerTypes = new List<string>
        {
            "tabs",
            "table",
            "simplecols2",
            "simplecols3",
            "simplecols4",
            "simplecontent",
            "simplepanel",
            "simpleparagraph",
            "simpletabs",
            "container",
            "columns"
        };

        private static readonly List<string> ColumnTypes = new List<string>
        {
            "simplecols2",
            "simplecols3",
            "simplecols4",
            "columns"
        };

        private static void AddComponentToDictionary(string key, string? tokenType, string label)
        {
            if (!components.ContainsKey(key))
            {
                var jsonValue = JsonConvert.SerializeObject(new { type = tokenType, label });
                components.Add(key, jsonValue);
            }
        }

        private static bool IsValidChildToken(JToken childToken)
        {
            var tokenInput = childToken["input"]?.ToString();
            var tokenType = childToken["type"]?.ToString();

            return tokenInput == "True" &&
                   tokenType != null &&
                   tokenType != "button" &&
                   !AllowableContainerTypes.Contains(tokenType);
        }

        public static string GetSubLookupType(string? tokenType)
        {
            // Default to "components" if tokenType is null or empty
            if (string.IsNullOrEmpty(tokenType))
            {
                return "components";
            }

            // Check if tokenType is part of ColumnTypes
            if (ColumnTypes.Contains(tokenType))
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

        public static void AddComponent(JToken childToken)
        {
            try
            {
                if (!IsValidChildToken(childToken)) return;

                string? key = childToken["key"]?.ToString();
                string? label = childToken["label"]?.ToString();
                string? tokenType = childToken["type"]?.ToString();

                if (key != null && label != null)
                {
                    AddComponentToDictionary(key, tokenType, label);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation(ex, "An exception occurred in {MethodName}: {ExceptionMessage}", nameof(AddComponent), ex.Message);
            }
        }

        public static void ConsumeToken(JToken? token)
        {
            if (token != null)
            {
                var subTokenType = token["type"]?.ToString();
                string subSubTokenString = GetSubLookupType(subTokenType);

                var nestedComponentsComponents = ((JObject)token).SelectToken(subSubTokenString);
                if (nestedComponentsComponents != null)
                {
                    GetAllInputComponents(nestedComponentsComponents);
                }
                else
                {
                    AddComponent(token);
                }
            }
        }

        public static void GetAllInputComponents(JToken? tokenComponents)
        {
            if (tokenComponents == null) return;

            foreach (var childToken in tokenComponents.Children<JToken>())
            {
                if (childToken.Type != JTokenType.Object) continue;

                ProcessChildToken(childToken);
            }
        }

        private static void ProcessChildToken(JToken childToken)
        {
            var tokenType = childToken["type"];

            // Add the component if applicable
            AddComponent(childToken);

            if (tokenType != null && AllowableContainerTypes.Contains(tokenType.ToString()))
            {
                ProcessNestedComponents(childToken, tokenType);
            }
            else
            {
                ConsumeToken(childToken);
            }
        }

        private static void ProcessNestedComponents(JToken childToken, JToken? tokenType)
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

        private static void ProcessNestedTokenComponent(JToken nestedTokenComponent, string subTokenString)
        {
            if (subTokenString == "rows")
            {
                GetAllInputComponents(nestedTokenComponent);
            }
            else
            {
                ConsumeToken(nestedTokenComponent);
            }
        }

        private static void FindNodes(JToken json, string name, List<JToken> nodes)
        {
            if (json.Type == JTokenType.Object)
            {
                foreach (JProperty child in json.Children<JProperty>())
                {
                    if (child.Name.StartsWith(name))
                    {
                        nodes.Add(child);
                    }
                    FindNodes(child.Value, name, nodes);
                }
            }
            else if (json.Type == JTokenType.Array)
            {
                foreach (JToken child in json.Children())
                {
                    FindNodes(child, name, nodes);
                }
            }
        }

        public static List<JToken> FindNodes(JToken json, string name)
        {
            var nodes = new List<JToken>();
            FindNodes(json, name, nodes);
            return nodes;
        }

    }
}
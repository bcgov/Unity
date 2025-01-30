using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Application.Services;
using Volo.Abp;
using Microsoft.Extensions.Logging;
using System;

namespace Unity.GrantManager.Reporting.DataGenerators
{
    [RemoteService(false)]
    public class ReportingDataGenerator : ApplicationService, IReportingDataGenerator
    {
        public string? Generate(dynamic formSubmission, string? reportKeys, Guid submissionId)
        {
            try
            {
                if (reportKeys == null) return null;

                var reportResult = new Dictionary<string, List<string>>();

                JObject submission = JObject.Parse(formSubmission.ToString());

                // Navigate to the "data" node within the "submission" node
                JToken? dataNode = submission.SelectToken("submission.submission.data");

                List<string> keysToTrack = [.. reportKeys.Split('|')];

                if (dataNode == null) return null;

                // Perform a recursive scan of the data node
                ScanNode(dataNode, keysToTrack, reportResult);

                // Ensure all keys are present in the result, even if no matches were found
                foreach (var key in keysToTrack)
                {
                    if (!reportResult.ContainsKey(key))
                    {
                        reportResult[key] = [];
                    }
                }

                // Clean up the JSON strings
                foreach (var key in reportResult.Keys.ToList())
                {
                    reportResult[key] = reportResult[key].Select(CleanJsonString).ToList();
                }

                // Sort the dictionary by keys alphabetically 
                var sortedResult = reportResult.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                // Create a simple JSON object from the result dictionary
                JObject jsonObject = [];

                foreach (var kvp in sortedResult)
                {
                    JArray valuesArray = new(kvp.Value);
                    jsonObject[kvp.Key] = valuesArray;
                }

                if (jsonObject == null) return null;

                // Prep value for db            
                return jsonObject.ToString();

            }
            catch (Exception ex)
            {
                // Blanket catch here, as we dont want this generation to interfere we intake, report formatted data can be re-generated later
                Logger.LogError(ex, "Error processing reporting data for submission - submissionId: {SubmissionId}", submissionId);
            }

            return null;
        }

        static void ScanNode(JToken node, List<string> keysToTrack, Dictionary<string, List<string>> result)
        {
            if (node.Type == JTokenType.Object)
            {
                foreach (var property in node.Children<JProperty>())
                {
                    // Check if the key is in the list of keys to track
                    if (keysToTrack.Contains(property.Name))
                    {
                        // Initialize the list if the key is not yet in the result dictionary
                        if (!result.TryGetValue(property.Name, out List<string>? value))
                        {
                            value = ([]);
                            result[property.Name] = value;
                        }

                        value.Add(property.Value.ToString());
                    }

                    // Recursively scan the property's value
                    ScanNode(property.Value, keysToTrack, result);
                }
            }
            else if (node.Type == JTokenType.Array)
            {
                foreach (var item in node.Children())
                {
                    // Recursively scan each item in the array
                    ScanNode(item, keysToTrack, result);
                }
            }
        }

        static string CleanJsonString(string jsonString)
        {
            if (IsJson(jsonString))
            {
                try
                {
                    var parsedJson = JsonConvert.DeserializeObject<JObject>(jsonString);

                    // Serialize the JObject back to a JSON string without escape characters
                    var cleaned = parsedJson != null ? parsedJson.ToString(Formatting.None) : jsonString;

                    return cleaned;
                }
                catch
                {
                    return jsonString; // Return as is if parsing fails
                }
            }
            return jsonString; // Return as is if not a JSON object
        }

        static bool IsJson(string str)
        {
            str = str.Trim();
            return (str.StartsWith('{') && str.EndsWith('}')) || (str.StartsWith('[') && str.EndsWith(']'));
        }
    }
}

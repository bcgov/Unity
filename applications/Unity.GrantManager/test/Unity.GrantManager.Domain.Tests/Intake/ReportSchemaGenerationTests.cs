using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using System.IO;
using System;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Xunit;
using System.Linq;
using System.Collections.Generic;

namespace Unity.GrantManager.Intake
{
    public class ReportSchemaGenerationTests : GrantManagerDomainTestBase
    {
        private readonly IIntakeFormSubmissionMapper _intakeFormSubmissionMapper;
        private readonly IApplicationChefsFileAttachmentRepository _applicationChefsFileAttachmentRepository;

        public ReportSchemaGenerationTests()
        {
            _applicationChefsFileAttachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
            _intakeFormSubmissionMapper = new IntakeFormSubmissionMapper(_applicationChefsFileAttachmentRepository);
        }

        private static dynamic? LoadTestData(string filename)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Intake\\Mapping\\" + filename);
            var reader = new StreamReader(filePath);
            var jsonStr = reader.ReadToEnd();
            var testData = JsonConvert.DeserializeObject<dynamic>(jsonStr);
            reader.Dispose();
            return testData;
        }


        [Theory]
        [InlineData("basicReportSchema.json", 128)]
        [InlineData("tableReportSchema.json", 10)]
        public void TestBasicDataGridMapping(string filename, int keyCount)
        {
            dynamic? formMapping = LoadTestData(filename);
            string result = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formMapping);
            JObject jObject = JObject.Parse(result);

            // Exclusion array
            string[] exclusionArray = ["simplebuttonadvanced", "datagrid"];

            var keys = jObject
                .Properties()
                .Where(p =>
                {
                    string? typeValue = JObject.Parse(p.Value.ToString())?["type"]?.ToString();
                    return typeValue != null && !exclusionArray.Contains(typeValue);
                })
                .Select(p => p.Name);


            string pipeDelimitedKeys = string.Join("|", keys);

            keys.Count().ShouldBe(keyCount);
        }

        [Theory]
        //InlineData("tableReportSchema.json", "tableReportSubmission.json")]
        [InlineData("reportingFieldsSchema.json", "reportingFieldsSubmission.json")]
        public void GenerateReportableData(string schemafilename, string submissionfilename)
        {
            // Get the schema keys
            var reportResult = new Dictionary<string, List<string>>();
            dynamic? formMapping = LoadTestData(schemafilename);

            string result = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formMapping);
            JObject jObject = JObject.Parse(result);

            result.ShouldNotBeNull();

            // Exclusion array
            string[] exclusionArray = ["simplebuttonadvanced", "datagrid"];
            string[] nestedKeyFields = ["simplecheckboxes", "simplecheckboxadvanced"];

            // Dictionary to store full key names and truncated key names
            Dictionary<string, string> keyMapping = [];

            // Filter out properties based on the exclusion array and extend child keys
            var keys = jObject
                .Properties()
                .SelectMany(p =>
                {

                    string? typeValue = JObject.Parse(p.Value.ToString())?["type"]?.ToString();
                    if (typeValue != null && exclusionArray.Contains(typeValue))
                    {
                        return [];
                    }

                    // Check for nested key fields and generate dashed keys
                    if (typeValue != null && nestedKeyFields.Contains(typeValue))
                    {
                        return ExtractNestedKeys(p);
                    }

                    return [p.Name];
                })
                .Distinct()
                .Select(fullKey =>
                {
                    string truncatedKey = fullKey.Length > 63 ? fullKey[..63] : fullKey;
                    keyMapping[fullKey] = truncatedKey; 
                    return fullKey;
                });

            // Get all keys and pipe separate them
            string pipeDelimitedKeys = string.Join("|", keys);

            // Truncate each key to a maximum of 63 characters and create a pipe-delimited string
            string truncatedDelimitedKeys = string.Join("|", keys.Select(k => k.Length > 63 ? k[..63] : k));

            // create a column map
            keys.ShouldNotContain("indicateWhichProcessListedBelowBestAlignsWithYourOrganizationsOperationsChooseAllThatApply");
            keys.ShouldContain("cultivationExtractionIELoggingPlantCultivationMining");
            keys.ShouldContain("advancedManufacturingTheUseOfInnovativeTechnologySuchAsRobotics3DPrintingAutomationOrOtherAdvancedTechnologiesToCreateValueAddedProductsSuchAsMassTimberOrBioproductsEtc");

            // Get the submission
            dynamic? submissionData = LoadTestData(submissionfilename);

            if (submissionData == null) Assert.Fail();

            JObject submission = JObject.Parse(submissionData.ToString());

            // Navigate to the "data" node within the "submission" node
            JToken? dataNode = submission.SelectToken("submission.submission.data");

            if (dataNode == null) Assert.Fail();

            List<string> keysToTrack = [.. pipeDelimitedKeys.Split('|')];

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

            jsonObject.ShouldNotBeNull();

            // Prep value for db            
            string cleanedJsonString = jsonObject.ToString();

            cleanedJsonString.ShouldNotBeNull();
        }

        private static string[] ExtractNestedKeys(JProperty jProperty)
        {
            if (jProperty == null) return [];

            string? valuesProp = JObject.Parse(jProperty.Value.ToString())?["values"]?.ToString();

            return string.IsNullOrEmpty(valuesProp) ? [] : valuesProp.ToString().Split(',');
        }

        static void DeepSearch(JToken node, string[] exclusionArray, List<string> keys)
        {
            if (node.Type == JTokenType.Object)
            {
                foreach (var property in node.Children<JProperty>())
                {
                    string? typeValue = property.Value["type"]?.ToString();
                    if (typeValue != null && exclusionArray.Contains(typeValue))
                    {
                        continue;
                    }

                    // Check if the node has a child of values which is an array of objects
                    if (typeValue == "simplecheckboxes" && property.Value["values"] is JArray valuesArray)
                    {
                        foreach (var valueObj in valuesArray)
                        {
                            keys.Add($"{property.Name}-{valueObj["value"]}");
                        }
                    }
                    else
                    {
                        // Add the key directly if no special handling is required
                        keys.Add(property.Name);
                    }

                    // Recursively search the child nodes
                    DeepSearch(property.Value, exclusionArray, keys);
                }
            }
            else if (node.Type == JTokenType.Array)
            {
                foreach (var item in node.Children())
                {
                    // Recursively search each item in the array
                    DeepSearch(item, exclusionArray, keys);
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
    }
}

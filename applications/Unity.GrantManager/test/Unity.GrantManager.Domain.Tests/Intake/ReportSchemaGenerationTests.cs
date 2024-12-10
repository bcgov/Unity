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
using Amazon.Runtime;

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
            var reportResult = new Dictionary<string, List<string>>();

            dynamic? formMapping = LoadTestData(schemafilename);
            JObject schemaMapping = JObject.Parse(formMapping); // need to work with this.
            
            string result = _intakeFormSubmissionMapper.InitializeAvailableFormFields(formMapping);
            JObject jObject = JObject.Parse(result);

            result.ShouldNotBeNull();

            // Exclusion array
            string[] exclusionArray = ["simplebuttonadvanced", "datagrid"];
            string[] complexSchema = ["simplecheckboxes", "simplecheckboxadvanced"];

            var keys = jObject.Properties()
                      .SelectMany(p =>
                      {
                          string? typeValue = JObject.Parse(p.Value.ToString())?["type"]?.ToString();
                          if (typeValue != null && exclusionArray.Contains(typeValue))
                          {
                              return Enumerable.Empty<string>();
                          }

                          // Check if the node has a child of values which is an array of objects
                          if (p.Value is JObject obj && obj["values"] is JArray valuesArray)
                          {
                              return valuesArray.Select(v => $"{p.Name}-{v["value"]}");
                          }

                          return [p.Name];
                      });


            string pipeDelimitedKeys = string.Join("|", keys);
            pipeDelimitedKeys += "|IshouldBeHere";

            // 

            // Get the schema
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

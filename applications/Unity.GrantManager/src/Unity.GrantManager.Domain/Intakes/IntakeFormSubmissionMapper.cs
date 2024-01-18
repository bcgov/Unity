using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Services;
using static OpenIddict.Abstractions.OpenIddictConstants;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Unity.GrantManager.Intakes
{
    public class IntakeFormSubmissionMapper : DomainService, IIntakeFormSubmissionMapper
    {
        private readonly Dictionary<string, string> components = new Dictionary<string, string>();

        public IntakeFormSubmissionMapper() { }

        private readonly List<string> AllowableContainerTypes = new List<string> (new string[] 
            {
                "tabs",            
                "simplecols2",
                "simplecols3",
                "simplecols4",
                "simplecontent",
                "simplepanel",
                "simpleparagraph",
                "simpletabs",
                "container",
                "columns" }
        );

        private readonly List<string> ColumnTypes = new List<string>(new string[]
        {
                "simplecols2",
                "simplecols3",
                "simplecols4",
                "columns" }
        );

        public void addComponent(JToken childToken)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            try
            {
                dynamic? tokenInput = childToken["input"];
                dynamic? tokenType = childToken["type"];

                if (tokenInput != null && tokenInput.ToString() == "True")
                {
                    dynamic? key = childToken["key"];
                    dynamic? label = childToken["label"];

                    if (key != null && label != null && tokenType != null && tokenType.ToString() != "button" && !AllowableContainerTypes.Contains(tokenType.ToString()))
                    {
                        var jsonValue = "{ \"type\": \"" + tokenType.ToString() + " \", \"label\":  \"" + label.ToString() + "\" }";
                        components.Add(key.ToString(), jsonValue);
                    }

                }
            } catch(Exception ex) 
            {
                Debug.WriteLine(ex.ToString());
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        public string GetSubLookupType(dynamic? tokenType)
        {
            string subTokenString = "components";
#pragma warning disable CS8602 
            if(tokenType != null && ColumnTypes.Contains(tokenType.ToString()))
            {
                subTokenString = "columns";
            }
#pragma warning restore CS8602
            return subTokenString;
        }

        public void ConsumeToken(JToken? token)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            if (token != null)
            {
                dynamic? subTokenType = token["type"];
                string subSubTokenString = GetSubLookupType(subTokenType);
                dynamic nestedComponentsComponents = ((JObject)token).SelectToken(subSubTokenString);
                if (nestedComponentsComponents != null)
                {
                    GetAllInputComponents(nestedComponentsComponents);
                }
                else
                {
                    addComponent(token);
                }
            }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }

        public void GetAllInputComponents(JToken? tokenComponents)
        {             
             // check if the type is in 'datagrid', 'editgrid', 'dynamicWizard' 
             // check the visibility comp._visible
             // check if the nestedComp.component.type equals 'panel'
            if (tokenComponents != null)
            {
                // Iterate through tokenComponents.ChildTokens
                foreach (JToken? childToken in tokenComponents.Children())
                {
                    if(childToken != null && childToken.Type == JTokenType.Object)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        dynamic? tokenInput = childToken["input"];
                        dynamic? tokenType = childToken["type"];
                        addComponent(childToken);

                        if (tokenType != null && AllowableContainerTypes.Contains(tokenType.ToString()))
                        {
                            string subTokenString = GetSubLookupType(tokenType);

                            // For each nested component container
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                            dynamic nestedTokenComponents = childToken.SelectToken(subTokenString);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                            if (nestedTokenComponents != null) { 
                                foreach (JToken nestedTokenComponent in nestedTokenComponents.Children())
                                {
                                    ConsumeToken(nestedTokenComponent);
                                }
                            }
                        } else
                        {
                            ConsumeToken(childToken);
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                }
            }
        }

        public string InitializeAvailableFormFields(dynamic formVersion)
        {
            // Check The Version of the form to make sure it is current
            JToken? tokenComponents = ((JObject)formVersion).SelectToken("schema.components");
            GetAllInputComponents(tokenComponents);
            return JsonSerializer.Serialize(components);
        }

        public IntakeMapping MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission, string? mapFormSubmissionFields)
        {
            var submission = formSubmission.submission;
            var data = submission.submission.data;
            var form = formSubmission.form;

            if (mapFormSubmissionFields != null)
            {
                try
                {
                    return ApplyConfigurationMapping(mapFormSubmissionFields!, data, form);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return ApplyDefaultConfigurationMapping(data, form);
                }
            }
            else
            {
                return ApplyDefaultConfigurationMapping(data, form);
            }
        }

        private static IntakeMapping ApplyDefaultConfigurationMapping(dynamic data, dynamic form)
        {
            return new IntakeMapping()
            {                
                ProjectName = form.name is string ? form.name : null,
                ApplicantName = data.applicantName is string ? data.applicantName : null,
                Sector = data.sector is string ? data.sector : null,
                TotalProjectBudget = data.totalProjectBudget is string ? data.totalProjectBudget : null,
                RequestedAmount = data.requestedAmount is string ? data.economicRegion : null,
                PhysicalCity = data.city is string ? data.city : null,
                EconomicRegion = data.economicRegion is string ? data.economicRegion : null,
            };
        }

        private static IntakeMapping ApplyConfigurationMapping(string submissionHeaderMapping, dynamic data, dynamic form)
        {
            var configMap = JsonConvert.DeserializeObject<dynamic>(submissionHeaderMapping)!;
            IntakeMapping intakeMapping = ApplyDefaultConfigurationMapping(data, form);

            if (configMap != null)
            {
                foreach (JProperty property in configMap.Properties())
                {
                    var dataKey = property.Name;
                    var intakeProperty = property.Value.ToString();
                    var dataValue = data.SelectToken(intakeProperty)?.ToString();

                    if (intakeProperty != null && dataValue != null && dataKey != null)
                    {
                        // Get a type object that represents the IntakeMapping.
                        Type intakeType = typeof(IntakeMapping);
                        PropertyInfo? intakePropInfo = intakeType.GetProperty(dataKey!);
                        intakePropInfo?.SetValue(intakeMapping, dataValue?.ToString());
                    }
                }
            }

            return intakeMapping;
        }
    }

    public static class MapperExtensions
    {
        public static JsonElement GetJsonElement(this JsonElement jsonElement, string path)
        {
            if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return default;

            string[] segments = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                if (int.TryParse(segment, out var index) && jsonElement.ValueKind == JsonValueKind.Array)
                {
                    jsonElement = jsonElement.EnumerateArray().ElementAtOrDefault(index);
                    if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                        return default;

                    continue;
                }

                jsonElement = jsonElement.TryGetProperty(segment, out var value) ? value : default;

                if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                    return default;
            }

            return jsonElement;
        }
    }
}

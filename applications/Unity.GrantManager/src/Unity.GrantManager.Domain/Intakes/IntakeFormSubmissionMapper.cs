using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Unity.GrantManager.Intakes
{
    public class IntakeFormSubmissionMapper : DomainService, IIntakeFormSubmissionMapper
    {
        private readonly Dictionary<string, string> components = new Dictionary<string, string>();
        private readonly IApplicationChefsFileAttachmentRepository _iApplicationChefsFileAttachmentRepository;

        public IntakeFormSubmissionMapper(IApplicationChefsFileAttachmentRepository iApplicationChefsFileAttachmentRepository)
        {
            _iApplicationChefsFileAttachmentRepository = iApplicationChefsFileAttachmentRepository;
        }

        private readonly List<string> AllowableContainerTypes = new List<string>(new string[]
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
            }
            catch (Exception ex)
            {
                // Duplicates are not an issue when adding the components 
                // as it is a hash if it exists already it should be ok just continue on
                Logger.LogException(ex);
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        public string GetSubLookupType(dynamic? tokenType)
        {
            string subTokenString = "components";
#pragma warning disable CS8602 
            if (tokenType != null && ColumnTypes.Contains(tokenType.ToString()))
            {
                subTokenString = "columns";
            }
            else if (tokenType != null && tokenType.ToString().Equals("table"))
            {
                subTokenString = "rows";
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
                    if (childToken != null && childToken.Type == JTokenType.Object)
                    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        dynamic? tokenType = childToken["type"];
                        addComponent(childToken);

                        if (tokenType != null && AllowableContainerTypes.Contains(tokenType.ToString()))
                        {
                            string subTokenString = GetSubLookupType(tokenType);

                            // For each nested component container
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                            dynamic nestedTokenComponents = childToken.SelectToken(subTokenString);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                            if (nestedTokenComponents != null)
                            {
                                foreach (JToken nestedTokenComponent in nestedTokenComponents.Children())
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
                            }
                        }
                        else
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

        public async Task SaveChefsFiles(dynamic formSubmission, Guid applicationId)
        {
            Dictionary<Guid, string> files = ExtractSubmissionFiles(formSubmission);
            var submissionId = formSubmission.submission.id;

            foreach (var file in files)
            {
                await _iApplicationChefsFileAttachmentRepository
                    .InsertAsync(new ApplicationChefsFileAttachment
                    {
                        ApplicationId = applicationId,
                        ChefsFileId = file.Key.ToString(),
                        ChefsSumbissionId = submissionId,
                        Name = file.Value,
                    });
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

        private static List<JToken> FindNodes(JToken json, string name)
        {
            var nodes = new List<JToken>();
            FindNodes(json, name, nodes);
            return nodes;
        }

        private static IntakeMapping ApplyDefaultConfigurationMapping(dynamic data, dynamic form)
        {
            return new IntakeMapping()
            {
                ApplicantName = data.applicantName is string ? data.applicantName : null,
                Sector = data.sector is string ? data.sector : null,
                TotalProjectBudget = data.totalProjectBudget is string ? data.totalProjectBudget : null,
                RequestedAmount = data.requestedAmount is string ? data.requestedAmount : null,
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

        public Dictionary<Guid, string> ExtractSubmissionFiles(dynamic formSubmission)
        {
            var files = new Dictionary<Guid, string>();

            var submission = formSubmission.submission;
            var data = submission.submission.data;
            var version = formSubmission.version;

            foreach (var fileKey in GetFileKeys(version))
            {
                var nodes = FindNodes(data, fileKey);

                foreach (JToken filesNode in nodes) //object containing array of files
                {
                    foreach (JToken prop in filesNode) //array of files
                    {
                        foreach (var obj in (JArray)prop) //each file in array
                        {
                            dynamic? fileObject = obj;
                            var id = fileObject.data.id;
                            var originalName = fileObject.originalName;
                            var uuid = Guid.Parse(Convert.ToString(id));
                            if (!files.ContainsKey(uuid))
                                files.Add(uuid, Convert.ToString(originalName));
                        }
                    }
                }
            }

            return files;
        }

        private List<string> GetFileKeys(dynamic version)
        {
            return FindFileKeys(version, "type", "simplefile");
        }

        private static List<string> FindFileKeys(JToken json, string key, string value)
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

        public async Task ResyncSubmissionAttachments(Guid applicationId, dynamic formSubmission)
        {
            await DeleteExistingChefsAttachmentRecords(applicationId);
            await SaveChefsFiles(formSubmission, applicationId);
        }

        private async Task DeleteExistingChefsAttachmentRecords(Guid applicationId)
        {
            var query = from chefsAttachment in await _iApplicationChefsFileAttachmentRepository.GetQueryableAsync()
                        where chefsAttachment.ApplicationId == applicationId
                        select chefsAttachment.Id;
            IList<Guid> chefsAttachmentsGuids = query.ToList();
            foreach (Guid chefsAttachmentGuid in chefsAttachmentsGuids)
            {
                await _iApplicationChefsFileAttachmentRepository.DeleteAsync(chefsAttachmentGuid);
            }
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

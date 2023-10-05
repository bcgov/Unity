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
using Volo.Abp.Uow;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Unity.GrantManager.Intakes
{
    public class IntakeFormSubmissionMapper : DomainService, IIntakeFormSubmissionMapper
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IApplicantRepository _applicantRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IApplicationFormSubmissionRepository _applicationFormSubmissionRepository;
        private Dictionary<string, string> components = new Dictionary<string, string>();

        public IntakeFormSubmissionMapper(IUnitOfWorkManager unitOfWorkManager,
            IApplicantRepository applicantRepository,
            IApplicationRepository applicationRepository,
            IApplicationStatusRepository applicationStatusRepository,
            IApplicationFormSubmissionRepository applicationFormSubmissionRepository)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _applicantRepository = applicantRepository;
            _applicationRepository = applicationRepository;
            _applicationStatusRepository = applicationStatusRepository; 
            _applicationFormSubmissionRepository = applicationFormSubmissionRepository;
        }

        public void getAllInputComponents(JToken? tokenComponents)
        {             
             //const dataArrayComponents = ['datagrid', 'editgrid', 'dynamicWizard'];
             //const visibleComponents = currentComponents.filter(comp => comp._visible);
             // if (nestedComp.component.type === 'panel') {
            if (tokenComponents != null)
            {
                // Iterate through tokenComponents.ChildTokens
                foreach (JToken childToken in tokenComponents.Children())
                {
                    if(childToken.Type == JTokenType.Object)
                    {
                        dynamic tokenInput = childToken["input"];
                        dynamic tokenType = childToken["type"];

                        if (tokenInput != null && tokenInput.ToString() == "True")
                        {
                            dynamic? key = childToken["key"];
                            dynamic? label = childToken["label"];

                            if (key != null && label != null && tokenType != null && tokenType.ToString() != "button")
                            {
                                var jsonValue = "{ \"type\": \""+ tokenType.ToString() + " \" \"label\":  \"" + label.ToString() + "\" }";
                                components.Add(key.ToString(), jsonValue);
                            }
                        }
                        else if (tokenType.ToString() == "tabs")
                        {
                            // For each nested component container
                            dynamic nestedTokenComponents = childToken.SelectToken("components");
                            foreach (JToken nestedTokenComponent in nestedTokenComponents.Children())
                            {
                                getAllInputComponents(((JObject)nestedTokenComponent).SelectToken("components"));
                            }
                        } else if (tokenType.ToString() == "columns") {
                            getAllInputComponents(childToken);
                        }
                    }
                }
            }
        }

        public async Task<string> InitializeAvailableFormFields(ApplicationForm applicationForm, dynamic formVersion)
        {
            // Check The Version of the form to make sure it is current
            JToken? tokenComponents = ((JObject)formVersion).SelectToken("schema.components");
            getAllInputComponents(tokenComponents);
            return JsonSerializer.Serialize(components);
        }

        public async Task<IntakeMapping> MapFormSubmissionFields(ApplicationForm applicationForm, dynamic formSubmission)
        {
            string? submissionHeaderMapping = applicationForm.SubmissionHeaderMapping;
            var submission = formSubmission.submission;
            var data = submission.submission.data;
            var form = submission.form;

            if (submissionHeaderMapping != null)
            {
                try
                {
                    return ApplyConfigurationMapping(submissionHeaderMapping!, data);
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
                ProjectName = form.name,
                ApplicantName = data.applicantName,
                Sector = data.sector,
                TotalProjectBudget = data.totalProjectBudget,
                RequestedAmount = data.requestedAmount
            };
        }

        private IntakeMapping ApplyConfigurationMapping(string submissionHeaderMapping, dynamic data)
        {
            var configMap = JsonConvert.DeserializeObject<dynamic>(submissionHeaderMapping)!;
            IntakeMapping intakeMapping = new IntakeMapping();
            if (configMap != null)
            {
                foreach (JProperty property in configMap.Properties())
                {
                    var dataKey = property.Name;
                    var intakeProperty = property.Value.ToString();
                    var dataValue = data.SelectToken(dataKey)?.ToString();

                    if (intakeProperty != null && dataValue != null)
                    {
                        // Get a type object that represents the IntakeMapping.
                        Type intakeType = typeof(IntakeMapping);

                        // Change the static property value.
                        PropertyInfo intakePropInfo = intakeType.GetProperty(intakeProperty);
                        if (intakePropInfo != null)
                        {
                            intakePropInfo.SetValue(intakeMapping, dataValue.Value.ToString());
                        }
                    }
                }
            }

            return intakeMapping;
        }
    }

    public class IntakeMapping
    {
        public string? ProjectName { get; set; }
        public string? ApplicantName { get; set; }
        public string? Sector { get; set; }
        public string? TotalProjectBudget { get; set; }
        public string? RequestedAmount { get; set; }
        public string? ConfirmationId { get; set; }
        public string? SubmissionId { get; set; }
        public string? SubmissionDate { get; set; }
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

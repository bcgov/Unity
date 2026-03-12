using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.AI
{
    public class ApplicationAnalysisService(
        IApplicationRepository applicationRepository,
        IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
        IApplicationFormVersionRepository applicationFormVersionRepository,
        IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
        IAIService aiService,
        ILogger<ApplicationAnalysisService> logger) : IApplicationAnalysisService, ITransientDependency
    {
        private readonly JsonSerializerOptions _jsonOptionsIndented = new()
        {
            WriteIndented = true
        };

        private const string ComponentsKey = "components";

        public async Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false)
        {
            var application = await applicationRepository.GetAsync(applicationId);
            var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
            var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);

            var attachmentSummaries = attachments
                .Where(a => !string.IsNullOrWhiteSpace(a.AISummary))
                .Select(a => new AIAttachmentItem
                {
                    Name = string.IsNullOrWhiteSpace(a.FileName) ? "attachment" : a.FileName.Trim(),
                    Summary = a.AISummary!.Trim()
                })
                .ToList();

            var notSpecified = "Not specified";
            var applicationContent = $@"
Project Name: {application.ProjectName}
Reference Number: {application.ReferenceNo}
Requested Amount: ${application.RequestedAmount:N2}
Total Project Budget: ${application.TotalProjectBudget:N2}
Project Summary: {application.ProjectSummary ?? "Not provided"}
City: {application.City ?? notSpecified}
Economic Region: {application.EconomicRegion ?? notSpecified}
Community: {application.Community ?? notSpecified}
Project Start Date: {application.ProjectStartDate?.ToShortDateString() ?? notSpecified}
Project End Date: {application.ProjectEndDate?.ToShortDateString() ?? notSpecified}
Submission Date: {application.SubmissionDate.ToShortDateString()}

FULL APPLICATION FORM SUBMISSION:
{formSubmission?.RenderedHTML ?? "Form submission content not available"}
";

            object formFieldConfiguration = new { message = "Form configuration not available." };
            if (formSubmission?.ApplicationFormVersionId != null)
            {
                formFieldConfiguration = await ExtractFormFieldConfigurationAsync(formSubmission.ApplicationFormVersionId.Value);
            }

            var analysis = await aiService.GenerateApplicationAnalysisAsync(new ApplicationAnalysisRequest
            {
                Schema = JsonSerializer.SerializeToElement(formFieldConfiguration),
                Data = JsonSerializer.SerializeToElement(new { submission_content = applicationContent }),
                Attachments = attachmentSummaries,
                PromptVersion = promptVersion,
                CapturePromptIo = capturePromptIo,
                CaptureContextId = applicationId.ToString()
            });

            var analysisJson = JsonSerializer.Serialize(analysis, _jsonOptionsIndented);
            application.AIAnalysis = analysisJson;
            await applicationRepository.UpdateAsync(application);
            return analysisJson;
        }

        private async Task<object> ExtractFormFieldConfigurationAsync(Guid formVersionId)
        {
            try
            {
                var formVersion = await applicationFormVersionRepository.GetAsync(formVersionId);
                if (formVersion == null || string.IsNullOrEmpty(formVersion.FormSchema))
                {
                    return new { message = "Form configuration not available." };
                }

                var schema = JObject.Parse(formVersion.FormSchema);
                var components = schema[ComponentsKey] as JArray;
                if (components == null || components.Count == 0)
                {
                    return new { message = "No form fields configured." };
                }

                var requiredFields = new List<string>();
                var optionalFields = new List<string>();
                ExtractFieldRequirements(components, requiredFields, optionalFields, string.Empty);

                return new
                {
                    required_fields = requiredFields,
                    optional_fields = optionalFields
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error extracting form field configuration for form version {FormVersionId}", formVersionId);
                return new { message = "Form configuration could not be extracted." };
            }
        }

        private static void ExtractFieldRequirements(JArray components, List<string> requiredFields, List<string> optionalFields, string currentPath)
        {
            foreach (var component in components.OfType<JObject>())
            {
                var key = component["key"]?.ToString();
                var label = component["label"]?.ToString();
                var type = component["type"]?.ToString();
                var skipTypes = new HashSet<string> { "button", "simplebuttonadvanced", "html", "htmlelement", "content", "simpleseparator" };

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(type) || skipTypes.Contains(type))
                {
                    ProcessNestedFieldRequirements(component, type, requiredFields, optionalFields, currentPath);
                    continue;
                }

                var displayName = !string.IsNullOrEmpty(label) ? $"{label} ({key})" : key;
                var fullPath = string.IsNullOrEmpty(currentPath) ? displayName : $"{currentPath} > {displayName}";
                var validate = component["validate"] as JObject;
                var isRequired = validate?["required"]?.Value<bool>() ?? false;

                if (component["input"]?.Value<bool>() == true)
                {
                    if (isRequired) requiredFields.Add(fullPath);
                    else optionalFields.Add(fullPath);
                }

                ProcessNestedFieldRequirements(component, type, requiredFields, optionalFields, fullPath);
            }
        }

        private static void ProcessNestedFieldRequirements(JObject component, string? type, List<string> requiredFields, List<string> optionalFields, string currentPath)
        {
            switch (type)
            {
                case "panel":
                case "simplepanel":
                case "fieldset":
                case "well":
                case "container":
                case "datagrid":
                case "table":
                    if (component[ComponentsKey] is JArray nestedComponents)
                    {
                        ExtractFieldRequirements(nestedComponents, requiredFields, optionalFields, currentPath);
                    }
                    break;
                case "columns":
                case "simplecols2":
                case "simplecols3":
                case "simplecols4":
                    if (component["columns"] is JArray columns)
                    {
                        foreach (var column in columns.OfType<JObject>())
                        {
                            if (column[ComponentsKey] is JArray columnComponents)
                            {
                                ExtractFieldRequirements(columnComponents, requiredFields, optionalFields, currentPath);
                            }
                        }
                    }
                    break;
                case "tabs":
                case "simpletabs":
                    if (component[ComponentsKey] is JArray tabs)
                    {
                        foreach (var tab in tabs.OfType<JObject>())
                        {
                            if (tab[ComponentsKey] is JArray tabComponents)
                            {
                                ExtractFieldRequirements(tabComponents, requiredFields, optionalFields, currentPath);
                            }
                        }
                    }
                    break;
            }
        }
    }
}

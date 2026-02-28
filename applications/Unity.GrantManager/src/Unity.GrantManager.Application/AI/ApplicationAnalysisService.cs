using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const string ComponentsKey = "components";
        private const string AnalysisRubric = @"
BC GOVERNMENT GRANT EVALUATION RUBRIC:

1. ELIGIBILITY REQUIREMENTS:
   - Project must align with program objectives
   - Applicant must be eligible entity type
   - Budget must be reasonable and well-justified
   - Project timeline must be realistic

2. COMPLETENESS CHECKS:
   - All required fields completed
   - Necessary supporting documents provided
   - Budget breakdown detailed and accurate
   - Project description clear and comprehensive

3. FINANCIAL REVIEW:
   - Requested amount is within program limits
   - Budget is reasonable for scope of work
   - Matching funds or in-kind contributions identified
   - Cost per outcome/beneficiary is reasonable

4. RISK ASSESSMENT:
   - Applicant capacity to deliver project
   - Technical feasibility of proposed work
   - Environmental or regulatory compliance
   - Potential for cost overruns or delays

5. QUALITY INDICATORS:
   - Clear project objectives and outcomes
   - Well-defined target audience/beneficiaries
   - Appropriate project methodology
   - Sustainability plan for long-term impact

EVALUATION CRITERIA:
- HIGH: Meets all requirements, well-prepared application, low risk
- MEDIUM: Meets most requirements, minor issues or missing elements
- LOW: Missing key requirements, significant concerns, high risk
";

        public async Task<string> RegenerateAndSaveAsync(Guid applicationId)
        {
            var application = await applicationRepository.GetAsync(applicationId);
            var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
            var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);

            var attachmentSummaries = attachments
                .Where(a => !string.IsNullOrWhiteSpace(a.AISummary))
                .Select(a => $"{a.FileName}: {a.AISummary}")
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

            string formFieldConfiguration = "Form configuration not available.";
            if (formSubmission?.ApplicationFormVersionId != null)
            {
                formFieldConfiguration = await ExtractFormFieldConfigurationAsync(formSubmission.ApplicationFormVersionId.Value);
            }

            var analysis = await aiService.AnalyzeApplicationAsync(
                applicationContent,
                attachmentSummaries,
                AnalysisRubric,
                formFieldConfiguration);

            var cleanedAnalysis = CleanJsonResponse(analysis);
            application.AIAnalysis = cleanedAnalysis;
            await applicationRepository.UpdateAsync(application);
            return cleanedAnalysis;
        }

        private static string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return response;

            var cleaned = response.Trim();

            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase) || cleaned.StartsWith("```"))
            {
                var startIndex = cleaned.IndexOf('\n');
                if (startIndex >= 0)
                {
                    cleaned = cleaned.Substring(startIndex + 1);
                }
            }

            if (cleaned.EndsWith("```"))
            {
                var lastIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (lastIndex > 0)
                {
                    cleaned = cleaned.Substring(0, lastIndex);
                }
            }

            return cleaned.Trim();
        }

        private async Task<string> ExtractFormFieldConfigurationAsync(Guid formVersionId)
        {
            try
            {
                var formVersion = await applicationFormVersionRepository.GetAsync(formVersionId);
                if (formVersion == null || string.IsNullOrEmpty(formVersion.FormSchema))
                {
                    return "Form configuration not available.";
                }

                var schema = JObject.Parse(formVersion.FormSchema);
                var components = schema[ComponentsKey] as JArray;
                if (components == null || components.Count == 0)
                {
                    return "No form fields configured.";
                }

                var requiredFields = new List<string>();
                var optionalFields = new List<string>();
                ExtractFieldRequirements(components, requiredFields, optionalFields, string.Empty);

                var configurationText = new StringBuilder();
                configurationText.AppendLine("FORM FIELD CONFIGURATION:");
                configurationText.AppendLine();

                if (requiredFields.Count > 0)
                {
                    configurationText.AppendLine("REQUIRED FIELDS (must be completed):");
                    foreach (var field in requiredFields)
                    {
                        configurationText.AppendLine($"- {field}");
                    }
                    configurationText.AppendLine();
                }

                if (optionalFields.Count > 0)
                {
                    configurationText.AppendLine("OPTIONAL FIELDS (may be left blank):");
                    foreach (var field in optionalFields)
                    {
                        configurationText.AppendLine($"- {field}");
                    }
                }

                return configurationText.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error extracting form field configuration for form version {FormVersionId}", formVersionId);
                return "Form configuration could not be extracted.";
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

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.AI.Prompts
{
    internal static class PromptDataPayloadBuilder
    {
        private static readonly string[] ExcludedPromptDataKeys =
        {
            "simplefile",
            "applicantAgent",
            "submit",
            "lateEntry",
            "metadata",
            "full_application_form_submission",
            "files",
            "file",
            "attachments"
        };

        private static readonly HashSet<string> NonDataComponentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "button",
            "simplebuttonadvanced",
            "html",
            "htmlelement",
            "content",
            "simpleseparator"
        };

        public static JsonElement BuildPromptDataPayload(
            Application application,
            ApplicationFormSubmission? formSubmission,
            string? formSchema,
            ILogger logger)
        {
            var fallbackPayload = BuildFallbackPromptDataPayload(application);
            if (TryBuildPromptDataValues(formSubmission?.Submission, formSchema, out var values, out var exception))
            {
                return JsonSerializer.SerializeToElement(values);
            }

            if (exception != null)
            {
                logger.LogWarning(
                    exception,
                    "Failed to parse form submission JSON for prompt payload generation for application {ApplicationId}.",
                    application.Id);
            }

            return JsonSerializer.SerializeToElement(fallbackPayload);
        }

        private static object BuildFallbackPromptDataPayload(Application application)
        {
            var notSpecified = "Not specified";
            return new
            {
                project_name = application.ProjectName,
                reference_number = application.ReferenceNo,
                requested_amount = application.RequestedAmount,
                total_project_budget = application.TotalProjectBudget,
                project_summary = application.ProjectSummary ?? "Not provided",
                city = application.City ?? notSpecified,
                economic_region = application.EconomicRegion ?? notSpecified,
                community = application.Community ?? notSpecified,
                project_start_date = application.ProjectStartDate,
                project_end_date = application.ProjectEndDate,
                submission_date = application.SubmissionDate
            };
        }

        private static bool TryBuildPromptDataValues(
            string? submissionJson,
            string? formSchema,
            out Dictionary<string, JsonElement> values,
            out Exception? exception)
        {
            values = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            exception = null;

            if (string.IsNullOrWhiteSpace(submissionJson))
            {
                return false;
            }

            try
            {
                using var submissionDoc = JsonDocument.Parse(submissionJson);
                if (!TryExtractSubmissionDataObject(submissionDoc.RootElement, out var submissionData))
                {
                    return false;
                }

                values = BuildPromptDataValues(submissionData, formSchema);
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        private static bool TryExtractSubmissionDataObject(JsonElement root, out JsonElement submissionData)
        {
            submissionData = root;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            {
                submissionData = dataElement;
                return true;
            }

            if (root.TryGetProperty("submission", out var submissionElement) &&
                submissionElement.ValueKind == JsonValueKind.Object &&
                submissionElement.TryGetProperty("data", out var nestedDataElement) &&
                nestedDataElement.ValueKind == JsonValueKind.Object)
            {
                submissionData = nestedDataElement;
                return true;
            }

            return true;
        }

        private static Dictionary<string, JsonElement> BuildPromptDataValues(JsonElement submissionData, string? formSchema)
        {
            var deserializedValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(submissionData.GetRawText()) ??
                                     new Dictionary<string, JsonElement>();
            var values = new Dictionary<string, JsonElement>(deserializedValues, StringComparer.OrdinalIgnoreCase);
            var allowedSchemaKeys = ExtractAllowedSchemaKeys(formSchema);

            foreach (var excludedKey in ExcludedPromptDataKeys)
            {
                values.Remove(excludedKey);
            }

            if (allowedSchemaKeys.Count > 0)
            {
                values = values.Where(kvp => allowedSchemaKeys.Contains(kvp.Key))
                               .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
            }

            return values;
        }

        private static HashSet<string> ExtractAllowedSchemaKeys(string? formSchema)
        {
            if (string.IsNullOrWhiteSpace(formSchema))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var schema = JObject.Parse(formSchema);
                if (schema["components"] is not JArray components)
                {
                    return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                ExtractSchemaKeys(components, keys);
                return keys;
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void ExtractSchemaKeys(JArray components, HashSet<string> keys)
        {
            foreach (var component in components.OfType<JObject>())
            {
                var key = component["key"]?.ToString();
                var type = component["type"]?.ToString();
                var isInput = component["input"]?.Value<bool>() == true;

                if (!string.IsNullOrWhiteSpace(key) &&
                    !string.IsNullOrWhiteSpace(type) &&
                    !NonDataComponentTypes.Contains(type) &&
                    isInput)
                {
                    keys.Add(key);
                }

                ProcessNestedSchemaComponents(component, keys);
            }
        }

        private static void ProcessNestedSchemaComponents(JObject component, HashSet<string> keys)
        {
            if (component["components"] is JArray nestedComponents)
            {
                ExtractSchemaKeys(nestedComponents, keys);
            }

            if (component["columns"] is JArray columns)
            {
                foreach (var column in columns.OfType<JObject>())
                {
                    if (column["components"] is JArray columnComponents)
                    {
                        ExtractSchemaKeys(columnComponents, keys);
                    }
                }
            }
        }
    }
}

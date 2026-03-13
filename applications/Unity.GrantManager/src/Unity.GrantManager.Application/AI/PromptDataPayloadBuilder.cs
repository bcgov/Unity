using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.AI
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

        public static JsonElement BuildPromptDataPayload(
            Application application,
            ApplicationFormSubmission? formSubmission,
            ILogger logger)
        {
            var fallbackPayload = BuildFallbackPromptDataPayload(application);
            if (TryBuildPromptDataValues(formSubmission?.Submission, out var values, out var exception))
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

                values = BuildPromptDataValues(submissionData);
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

        private static Dictionary<string, JsonElement> BuildPromptDataValues(JsonElement submissionData)
        {
            var deserializedValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(submissionData.GetRawText()) ??
                                     new Dictionary<string, JsonElement>();
            var values = new Dictionary<string, JsonElement>(deserializedValues, StringComparer.OrdinalIgnoreCase);

            foreach (var excludedKey in ExcludedPromptDataKeys)
            {
                values.Remove(excludedKey);
            }

            return values;
        }
    }
}

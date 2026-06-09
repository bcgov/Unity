using System;
using System.Collections.Generic;
using System.Text.Json;
using Unity.AI.Models;

namespace Unity.AI.Runtime
{
    public static class AIProviderPayloadValidator
    {
        public static AIResponseValidationResult ValidateAttachmentSummaryText(string response)
        {
            return !string.IsNullOrWhiteSpace(response)
                ? AIResponseValidationResult.Success()
                : AIResponseValidationResult.Invalid("Attachment summary response was empty.");
        }

        public static AIResponseValidationResult ValidateApplicationAnalysisJson(string response)
        {
            if (!TryParseRootObject(response, out var root))
            {
                return AIResponseValidationResult.Invalid("Application analysis response was not valid JSON.");
            }

            if (!root.TryGetProperty(AIJsonKeys.Decision, out var decision) || decision.ValueKind != JsonValueKind.String)
            {
                return AIResponseValidationResult.Invalid($"Application analysis response is missing required field: {AIJsonKeys.Decision}.");
            }

            if (!root.TryGetProperty(AIJsonKeys.Errors, out var errors) || errors.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid($"Application analysis response is missing required field: {AIJsonKeys.Errors}.");
            }

            if (!root.TryGetProperty(AIJsonKeys.Warnings, out var warnings) || warnings.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid($"Application analysis response is missing required field: {AIJsonKeys.Warnings}.");
            }

            if (!root.TryGetProperty(AIJsonKeys.Summaries, out var summaries) || summaries.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid($"Application analysis response is missing required field: {AIJsonKeys.Summaries}.");
            }

            if (!root.TryGetProperty(AIJsonKeys.Recommendations, out var recommendations) || recommendations.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid($"Application analysis response is missing required field: {AIJsonKeys.Recommendations}.");
            }

            return AIResponseValidationResult.Success();
        }

        public static AIResponseValidationResult ValidateApplicationScoringJson(string response, string sectionJson)
        {
            if (!TryParseRootObject(response, out var root))
            {
                return AIResponseValidationResult.Invalid("Application scoring response was not valid JSON.");
            }

            var expectedQuestionIds = ExtractQuestionIds(sectionJson);
            if (expectedQuestionIds.Count == 0)
            {
                return AIResponseValidationResult.Invalid("Application scoring section schema did not contain any question ids.");
            }

            foreach (var questionId in expectedQuestionIds)
            {
                if (!root.TryGetProperty(questionId, out var answerObject) || answerObject.ValueKind != JsonValueKind.Object)
                {
                    return AIResponseValidationResult.Invalid(
                        $"Application scoring response is missing required answer object for question id '{questionId}'.");
                }

                if (!answerObject.TryGetProperty(AIJsonKeys.Answer, out var answerValue)
                    || answerValue.ValueKind == JsonValueKind.Null
                    || answerValue.ValueKind == JsonValueKind.Object
                    || answerValue.ValueKind == JsonValueKind.Array)
                {
                    return AIResponseValidationResult.Invalid(
                        $"Application scoring response is missing a valid answer for question id '{questionId}'.");
                }

                if (!answerObject.TryGetProperty(AIJsonKeys.Confidence, out var confidenceValue)
                    || confidenceValue.ValueKind != JsonValueKind.Number
                    || !confidenceValue.TryGetInt32(out var confidence)
                    || confidence < 0
                    || confidence > 100)
                {
                    return AIResponseValidationResult.Invalid(
                        $"Application scoring response is missing a valid confidence score for question id '{questionId}'.");
                }
            }

            return AIResponseValidationResult.Success();
        }

        private static HashSet<string> ExtractQuestionIds(string sectionJson)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var jsonDoc = JsonDocument.Parse(sectionJson);
                var root = jsonDoc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    AddQuestionIds(root, ids);
                    return ids;
                }

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("questions", out var questionsElement) &&
                    questionsElement.ValueKind == JsonValueKind.Array)
                {
                    AddQuestionIds(questionsElement, ids);
                }
            }
            catch
            {
                return ids;
            }

            return ids;
        }

        private static void AddQuestionIds(JsonElement questionsArray, HashSet<string> ids)
        {
            foreach (var item in questionsArray.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("id", out var idProperty) ||
                    idProperty.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var id = idProperty.GetString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }
        }

        private static bool TryParseRootObject(string response, out JsonElement root)
        {
            root = default;

            if (string.IsNullOrWhiteSpace(response))
            {
                return false;
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(AIResponseJson.CleanJsonResponse(response));
                if (jsonDoc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                root = jsonDoc.RootElement.Clone();
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}

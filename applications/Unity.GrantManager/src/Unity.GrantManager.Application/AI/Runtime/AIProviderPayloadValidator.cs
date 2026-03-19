using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Unity.GrantManager.AI
{
    internal static class AIProviderPayloadValidator
    {
        public static bool IsValidAttachmentSummaryText(string response)
        {
            return !string.IsNullOrWhiteSpace(response);
        }

        public static bool IsValidApplicationAnalysisJson(string response)
        {
            if (!TryParseRootObject(response, out var root))
            {
                return false;
            }

            return root.TryGetProperty(AIJsonKeys.Rating, out var rating)
                   && rating.ValueKind == JsonValueKind.String
                   && root.TryGetProperty(AIJsonKeys.Errors, out var errors)
                   && errors.ValueKind == JsonValueKind.Array
                   && root.TryGetProperty(AIJsonKeys.Warnings, out var warnings)
                   && warnings.ValueKind == JsonValueKind.Array
                   && root.TryGetProperty(AIJsonKeys.Summaries, out var summaries)
                   && summaries.ValueKind == JsonValueKind.Array
                   && root.TryGetProperty(AIJsonKeys.NextSteps, out var nextSteps)
                   && nextSteps.ValueKind == JsonValueKind.Array;
        }

        public static bool IsValidApplicationScoringJson(string response, string sectionJson)
        {
            if (!TryParseRootObject(response, out var root))
            {
                return false;
            }

            var expectedQuestionIds = ExtractQuestionIds(sectionJson);
            if (expectedQuestionIds.Count == 0)
            {
                return false;
            }

            foreach (var questionId in expectedQuestionIds)
            {
                if (!root.TryGetProperty(questionId, out var answerObject) || answerObject.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                if (!answerObject.TryGetProperty(AIJsonKeys.Answer, out var answerValue)
                    || answerValue.ValueKind == JsonValueKind.Null
                    || answerValue.ValueKind == JsonValueKind.Object
                    || answerValue.ValueKind == JsonValueKind.Array)
                {
                    return false;
                }

                if (!answerObject.TryGetProperty(AIJsonKeys.Confidence, out var confidenceValue)
                    || confidenceValue.ValueKind != JsonValueKind.Number
                    || !confidenceValue.TryGetInt32(out var confidence)
                    || confidence < 0
                    || confidence > 100)
                {
                    return false;
                }
            }

            return true;
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


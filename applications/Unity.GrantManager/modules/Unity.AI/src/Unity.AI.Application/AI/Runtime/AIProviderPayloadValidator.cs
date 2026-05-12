using System;
using System.Collections.Generic;
using System.Text.Json;
using Unity.AI.Models;

namespace Unity.AI.Runtime
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

            return HasStringProperty(root, AIJsonKeys.Decision) &&
                   HasArrayProperty(root, AIJsonKeys.Errors) &&
                   HasArrayProperty(root, AIJsonKeys.Warnings) &&
                   HasArrayProperty(root, AIJsonKeys.Summaries) &&
                   HasArrayProperty(root, AIJsonKeys.Recommendations);
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
                if (!TryGetRequiredObject(root, questionId, out var answerObject))
                {
                    return false;
                }

                if (!HasPrimitiveProperty(answerObject, AIJsonKeys.Answer))
                {
                    return false;
                }

                if (!IsValidConfidenceProperty(answerObject, AIJsonKeys.Confidence))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasStringProperty(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var property) &&
                   property.ValueKind == JsonValueKind.String;
        }

        private static bool HasArrayProperty(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var property) &&
                   property.ValueKind == JsonValueKind.Array;
        }

        private static bool TryGetRequiredObject(JsonElement element, string name, out JsonElement value)
        {
            return element.TryGetProperty(name, out value) &&
                   value.ValueKind == JsonValueKind.Object;
        }

        private static bool HasPrimitiveProperty(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var property) &&
                   property.ValueKind != JsonValueKind.Null &&
                   property.ValueKind != JsonValueKind.Object &&
                   property.ValueKind != JsonValueKind.Array;
        }

        private static bool IsValidConfidenceProperty(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var property) &&
                   property.ValueKind == JsonValueKind.Number &&
                   property.TryGetInt32(out var confidence) &&
                   confidence >= 0 &&
                   confidence <= 100;
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

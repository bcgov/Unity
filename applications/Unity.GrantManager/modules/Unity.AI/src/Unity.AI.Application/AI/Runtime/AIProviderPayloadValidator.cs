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

            if (!root.TryGetProperty("decision", out var decision) || decision.ValueKind != JsonValueKind.String)
            {
                return AIResponseValidationResult.Invalid("Application analysis response is missing or invalid required field 'decision' (expected string).");
            }

            var normalizedDecision = (decision.GetString() ?? string.Empty).Trim().ToUpperInvariant();
            if (normalizedDecision != "PROCEED" && normalizedDecision != "HOLD")
            {
                return AIResponseValidationResult.Invalid(
                    "Application analysis response has invalid 'decision' value. Expected 'PROCEED' or 'HOLD'.");
            }

            if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid("Application analysis response is missing or invalid required field 'errors' (expected array).");
            }

            if (!root.TryGetProperty("warnings", out var warnings) || warnings.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid("Application analysis response is missing or invalid required field 'warnings' (expected array).");
            }

            if (!root.TryGetProperty("summaries", out var summaries) || summaries.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid("Application analysis response is missing or invalid required field 'summaries' (expected array).");
            }

            if (!root.TryGetProperty("recommendations", out var recommendations) || recommendations.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid("Application analysis response is missing or invalid required field 'recommendations' (expected array).");
            }

            if (summaries.GetArrayLength() == 0)
            {
                return AIResponseValidationResult.Invalid(
                    "Application analysis response must include at least one item in 'summaries'.");
            }

            if (recommendations.GetArrayLength() == 0)
            {
                return AIResponseValidationResult.Invalid(
                    "Application analysis response must include at least one item in 'recommendations'.");
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
                return AIResponseValidationResult.Invalid("Application scoring section schema could not be parsed or did not contain any question ids.");
            }

            foreach (var questionId in expectedQuestionIds)
            {
                if (!root.TryGetProperty(questionId, out var answerObject) || answerObject.ValueKind != JsonValueKind.Object)
                {
                    return AIResponseValidationResult.Invalid(
                        $"Application scoring response is missing required answer object for question id '{questionId}'.");
                }

                if (!answerObject.TryGetProperty("answer", out var answerValue)
                    || answerValue.ValueKind == JsonValueKind.Null
                    || answerValue.ValueKind == JsonValueKind.Object
                    || answerValue.ValueKind == JsonValueKind.Array)
                {
                    return AIResponseValidationResult.Invalid(
                        $"Application scoring response is missing a valid answer for question id '{questionId}'.");
                }

                if (!answerObject.TryGetProperty("confidence", out var confidenceValue)
                    || confidenceValue.ValueKind != JsonValueKind.Number
                    || !confidenceValue.TryGetDecimal(out var confidence)
                    || confidence < 0m
                    || confidence > 1m)
                {
                    return AIResponseValidationResult.Invalid(
                        $"Application scoring response is missing a valid confidence score for question id '{questionId}'.");
                }
            }

            return AIResponseValidationResult.Success();
        }

        public static AIResponseValidationResult ValidateFormMappingJson(string response)
        {
            if (!TryParseRootObject(response, out _))
            {
                return AIResponseValidationResult.Invalid("Mapping suggestion response was not valid JSON.");
            }

            return AIResponseValidationResult.Success();
        }

        public static AIResponseValidationResult ValidateFormWorksheetJson(string response)
        {
            if (!TryParseRootObject(response, out var root))
            {
                return AIResponseValidationResult.Invalid("Form worksheet response was not valid JSON.");
            }

            if (!root.TryGetProperty("title", out var title)
                || title.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(title.GetString()))
            {
                return AIResponseValidationResult.Invalid("Form worksheet response is missing a non-empty 'title'.");
            }

            if (!root.TryGetProperty("sections", out var sections)
                || sections.ValueKind != JsonValueKind.Array
                || sections.GetArrayLength() == 0)
            {
                return AIResponseValidationResult.Invalid("Form worksheet response must include at least one section.");
        }

        public static AIResponseValidationResult ValidateFormScoresheetJson(string response)
        {
            if (!TryParseRootObject(response, out var root))
            {
                return AIResponseValidationResult.Invalid("Scoresheet response was not valid JSON.");
            }

            foreach (var propertyName in new[] { "Title", "Name", "Version", "Order", "Published", "ReportColumns", "ReportKeys", "ReportViewName" })
            {
                var result = ValidateRequiredProperty(root, propertyName, "scoresheet");
                if (!result.IsValid)
                {
                    return result;
                }
            }

            return ValidateSections(root, "scoresheet", requiresFieldKey: false, allowEmptyFields: false);
        }

        private static AIResponseValidationResult ValidateSections(JsonElement root, string responseName, bool requiresFieldKey, bool allowEmptyFields)
        {
            if (!TryGetProperty(root, "Sections", out var sections) || sections.ValueKind != JsonValueKind.Array)
            {
                return AIResponseValidationResult.Invalid($"{responseName} response is missing or invalid required field 'Sections' (expected array).");
            }

            if (sections.GetArrayLength() == 0)
            {
                return AIResponseValidationResult.Invalid($"{responseName} response must include at least one section.");
            }

            foreach (var section in sections.EnumerateArray())
            {
                if (section.ValueKind != JsonValueKind.Object)
                {
                    return AIResponseValidationResult.Invalid($"{responseName} response contains an invalid section (expected object).");
                }

                foreach (var propertyName in new[] { "Name", "Order" })
                {
                    var result = ValidateRequiredProperty(section, propertyName, $"{responseName} section");
                    if (!result.IsValid)
                    {
                        return result;
                    }
                }

                if (!TryGetProperty(section, "Fields", out var fields) || fields.ValueKind != JsonValueKind.Array)
                {
                    return AIResponseValidationResult.Invalid($"{responseName} response contains a section without valid 'Fields' (expected array).");
                }

                if (!allowEmptyFields && fields.GetArrayLength() == 0)
                {
                    return AIResponseValidationResult.Invalid($"{responseName} response contains a section without fields.");
                }

                foreach (var field in fields.EnumerateArray())
                {
                    var result = ValidateField(field, responseName, requiresFieldKey);
                    if (!result.IsValid)
                    {
                        return result;
                    }
                }
            }

            return AIResponseValidationResult.Success();
        }

        private static AIResponseValidationResult ValidateField(JsonElement field, string responseName, bool requiresFieldKey)
        {
            if (field.ValueKind != JsonValueKind.Object)
            {
                return AIResponseValidationResult.Invalid($"{responseName} response contains an invalid field (expected object).");
            }

            var requiredFields = requiresFieldKey
                ? new[] { "Key", "Label", "Type", "Definition" }
                : new[] { "Name", "Label", "Order", "Type", "Definition" };

            foreach (var propertyName in requiredFields)
            {
                var result = ValidateRequiredProperty(field, propertyName, $"{responseName} field");
                if (!result.IsValid)
                {
                    return result;
                }
            }

            var definitionResult = ValidateDefinitionProperty(field, responseName);
            if (!definitionResult.IsValid)
            {
                return definitionResult;
            }

            return AIResponseValidationResult.Success();
        }

        private static AIResponseValidationResult ValidateDefinitionProperty(JsonElement field, string responseName)
        {
            if (!TryGetProperty(field, "Definition", out var definition) || definition.ValueKind == JsonValueKind.Null)
            {
                return AIResponseValidationResult.Invalid($"{responseName} response contains a field without a Definition.");
            }

            if (definition.ValueKind != JsonValueKind.String)
            {
                return AIResponseValidationResult.Invalid($"{responseName} response field Definition must be a JSON object encoded as a string.");
            }

            var definitionText = definition.GetString();
            if (string.IsNullOrWhiteSpace(definitionText))
            {
                return AIResponseValidationResult.Invalid($"{responseName} response field Definition cannot be empty.");
            }

            try
            {
                using var definitionDocument = JsonDocument.Parse(definitionText);
                if (definitionDocument.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return AIResponseValidationResult.Invalid($"{responseName} response field Definition must contain a JSON object.");
                }
            }
            catch (JsonException)
            {
                return AIResponseValidationResult.Invalid($"{responseName} response field Definition must contain valid JSON.");
            }

            return AIResponseValidationResult.Success();
        }

        private static AIResponseValidationResult ValidateRequiredProperty(JsonElement element, string propertyName, string sourceName)
        {
            if (!TryGetProperty(element, propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            {
                return AIResponseValidationResult.Invalid($"{sourceName} response is missing required field '{propertyName}'.");
            }

            return AIResponseValidationResult.Success();
        }

        private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out property))
            {
                return true;
            }

            property = default;
            return false;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Unity.AI.Models;
using Unity.AI.Responses;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIResponseParser : ITransientDependency
{
    public static ApplicationAnalysisResponse ParseApplicationAnalysisResponse(string raw)
    {
        var response = new ApplicationAnalysisResponse();
        if (!TryParseJsonObjectFromResponse(AddIdsToAnalysisItems(raw), out var root))
        {
            return response;
        }

        if (TryGetStringProperty(root, AIJsonKeys.Decision, out var decision))
        {
            response.Decision = decision.Trim().ToUpperInvariant();
        }

        if (TryGetArrayProperty(root, AIJsonKeys.Errors, out var errorsArray))
        {
            response.Errors = ParseFindings(errorsArray).ToList();
        }

        if (TryGetArrayProperty(root, AIJsonKeys.Warnings, out var warningsArray))
        {
            response.Warnings = ParseFindings(warningsArray).ToList();
        }

        if (TryGetArrayProperty(root, AIJsonKeys.Summaries, out var summariesArray))
        {
            response.Summaries = ParseFindings(summariesArray).ToList();
        }

        if (TryGetArrayProperty(root, AIJsonKeys.Recommendations, out var recommendationsArray))
        {
            response.Recommendations = ParseFindings(recommendationsArray).ToList();
        }

        return response;
    }

    public static AttachmentSummaryBatchResponse ParseAttachmentSummaryBatchResponse(string raw)
    {
        var response = new AttachmentSummaryBatchResponse();
        if (!TryParseJsonObjectFromResponse(raw, out var root))
        {
            return response;
        }

        if (!root.TryGetProperty("attachments", out var attachments) || attachments.ValueKind != JsonValueKind.Array)
        {
            return response;
        }

        foreach (var attachment in attachments.EnumerateArray())
        {
            if (attachment.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var attachmentId = attachment.TryGetProperty("attachmentId", out var idProp) && idProp.ValueKind == JsonValueKind.String
                ? idProp.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(attachmentId))
            {
                continue;
            }

            var summary = attachment.TryGetProperty(AIJsonKeys.Summary, out var summaryProp) && summaryProp.ValueKind == JsonValueKind.String
                ? summaryProp.GetString() ?? string.Empty
                : string.Empty;

            response.Attachments.Add(new AttachmentSummaryBatchItemResponse
            {
                AttachmentId = attachmentId,
                Summary = summary
            });
        }

        return response;
    }

    public static ApplicationScoringResponse ParseApplicationScoringResponse(string raw, IReadOnlyDictionary<string, string>? questionIdAliasMap = null)
    {
        var response = new ApplicationScoringResponse();
        if (!TryParseJsonObjectFromResponse(raw, out var root))
        {
            return response;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var answer = property.Value.TryGetProperty("answer", out var answerProp)
                ? answerProp.Clone()
                : default;
            var rationale = property.Value.TryGetProperty("rationale", out var rationaleProp) &&
                            rationaleProp.ValueKind == JsonValueKind.String
                ? rationaleProp.GetString() ?? string.Empty
                : string.Empty;
            var confidence = property.Value.TryGetProperty("confidence", out var confidenceProp) &&
                             confidenceProp.ValueKind == JsonValueKind.Number &&
                             confidenceProp.TryGetDecimal(out var parsedConfidence)
                ? NormalizeConfidence(parsedConfidence)
                : 0;

            var questionId = questionIdAliasMap != null &&
                             questionIdAliasMap.TryGetValue(property.Name, out var originalQuestionId)
                ? originalQuestionId
                : property.Name;

            response.Answers[questionId] = new ApplicationScoringAnswer
            {
                Answer = answer,
                Rationale = rationale,
                Confidence = confidence
            };
        }

        return response;
    }

    public static MappingSuggestionResponse ParseMappingSuggestionResponse(string raw)
    {
        var response = new MappingSuggestionResponse();
        if (!TryParseJsonObjectFromResponse(raw, out var root))
        {
            return response;
        }

        if (root.TryGetProperty("coreFieldMatches", out var coreFieldMatches) && coreFieldMatches.ValueKind == JsonValueKind.Array)
        {
            response.CoreFieldMatches = ParseMappingSuggestionItems(coreFieldMatches).ToList();
        }

        if (root.TryGetProperty("worksheetMatches", out var worksheetMatches) && worksheetMatches.ValueKind == JsonValueKind.Array)
        {
            response.WorksheetMatches = worksheetMatches.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => new WorksheetMappingSuggestionResponse
                {
                    WorksheetName = item.TryGetProperty("worksheetName", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? string.Empty : string.Empty,
                    FieldMatches = item.TryGetProperty("fieldMatches", out var matches) && matches.ValueKind == JsonValueKind.Array
                        ? ParseMappingSuggestionItems(matches).ToList()
                        : []
                })
                .ToList();
        }

        if (root.TryGetProperty("worksheetCreationSuggestions", out var worksheetCreationSuggestions) && worksheetCreationSuggestions.ValueKind == JsonValueKind.Array)
        {
            response.WorksheetCreationSuggestions = worksheetCreationSuggestions.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => new WorksheetCreationSuggestionResponse
                {
                    WorksheetName = item.TryGetProperty("worksheetName", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? string.Empty : string.Empty,
                    Reason = item.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String ? reason.GetString() ?? string.Empty : string.Empty,
                    SuggestedFields = item.TryGetProperty("suggestedFields", out var fields) && fields.ValueKind == JsonValueKind.Array
                        ? ParseMappingFields(fields).ToList()
                        : []
                })
                .ToList();
        }

        if (root.TryGetProperty("issues", out var issues) && issues.ValueKind == JsonValueKind.Array)
        {
            response.Issues = issues.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => new MappingIssueResponse
                {
                    Code = item.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String ? code.GetString() ?? string.Empty : string.Empty,
                    Message = item.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String ? message.GetString() ?? string.Empty : string.Empty
                })
                .ToList();
        }

        return response;
    }

    private static string AddIdsToAnalysisItems(string analysisJson)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(analysisJson);
            using var memoryStream = new System.IO.MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    var outputPropertyName = property.Name;

                    if (outputPropertyName == AIJsonKeys.Errors ||
                        outputPropertyName == AIJsonKeys.Warnings ||
                        outputPropertyName == AIJsonKeys.Summaries ||
                        outputPropertyName == AIJsonKeys.Recommendations)
                    {
                        writer.WritePropertyName(outputPropertyName);
                        writer.WriteStartArray();

                        foreach (var item in property.Value.EnumerateArray())
                        {
                            writer.WriteStartObject();

                            foreach (var itemProperty in item.EnumerateObject())
                            {
                                itemProperty.WriteTo(writer);
                            }

                            if (!item.TryGetProperty(AIJsonKeys.Id, out var idProp) ||
                                idProp.ValueKind != JsonValueKind.String ||
                                string.IsNullOrWhiteSpace(idProp.GetString()))
                            {
                                writer.WriteString(AIJsonKeys.Id, Guid.NewGuid().ToString());
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndArray();
                        continue;
                    }

                    property.WriteTo(writer);
                }

                writer.WriteEndObject();
                writer.Flush();
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch
        {
            return analysisJson;
        }
    }

    private static IEnumerable<ApplicationAnalysisFinding> ParseFindings(JsonElement findingsArray)
    {
        foreach (var item in findingsArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var id = Guid.NewGuid().ToString();
            if (item.TryGetProperty(AIJsonKeys.Id, out var idProp) && idProp.ValueKind == JsonValueKind.String)
            {
                id = idProp.GetString() ?? id;
            }

            var dismissed = item.TryGetProperty(AIJsonKeys.Dismissed, out var dismissedProp) &&
                (dismissedProp.ValueKind == JsonValueKind.True || dismissedProp.ValueKind == JsonValueKind.False) &&
                dismissedProp.GetBoolean();

            string? title = null;
            if (item.TryGetProperty(AIJsonKeys.Title, out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
            {
                title = titleProp.GetString();
            }

            string? detail = null;
            if (item.TryGetProperty(AIJsonKeys.Detail, out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
            {
                detail = detailProp.GetString();
            }

            yield return new ApplicationAnalysisFinding
            {
                Id = id,
                Dismissed = dismissed,
                Title = title,
                Detail = detail
            };
        }
    }

    private static IEnumerable<MappingSuggestionItemResponse> ParseMappingSuggestionItems(JsonElement itemsArray)
    {
        foreach (var item in itemsArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            yield return new MappingSuggestionItemResponse
            {
                SourceField = item.TryGetProperty("sourceField", out var sourceField) && sourceField.ValueKind == JsonValueKind.String ? sourceField.GetString() ?? string.Empty : string.Empty,
                TargetField = item.TryGetProperty("targetField", out var targetField) && targetField.ValueKind == JsonValueKind.String ? targetField.GetString() ?? string.Empty : string.Empty,
                Reason = item.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String ? reason.GetString() ?? string.Empty : string.Empty,
                Confidence = item.TryGetProperty("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number && confidence.TryGetDecimal(out var parsedConfidence) ? parsedConfidence : 0m
            };
        }
    }

    private static IEnumerable<MappingFieldResponse> ParseMappingFields(JsonElement itemsArray)
    {
        foreach (var item in itemsArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            yield return new MappingFieldResponse
            {
                Name = item.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? string.Empty : string.Empty,
                Type = item.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String ? type.GetString() ?? string.Empty : string.Empty,
                Label = item.TryGetProperty("label", out var label) && label.ValueKind == JsonValueKind.String ? label.GetString() ?? string.Empty : string.Empty,
                IsCustom = item.TryGetProperty("isCustom", out var isCustom) && (isCustom.ValueKind == JsonValueKind.True || isCustom.ValueKind == JsonValueKind.False) && isCustom.GetBoolean()
            };
        }
    }

    private static bool TryParseJsonObjectFromResponse(string response, out JsonElement objectElement)
    {
        objectElement = default;
        var cleaned = AIResponseJson.CleanJsonResponse(response);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            objectElement = doc.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = prop.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryGetArrayProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        value = prop.Clone();
        return true;
    }

    private static int NormalizeConfidence(decimal confidence)
    {
        var clamped = Math.Clamp(confidence, 0m, 1m);
        var percentage = clamped * 100m;
        var rounded = (int)Math.Round(percentage / 10m, MidpointRounding.AwayFromZero) * 10;
        return Math.Clamp(rounded, 0, 100);
    }
}

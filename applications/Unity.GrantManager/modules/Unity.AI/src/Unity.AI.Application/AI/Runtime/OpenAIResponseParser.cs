using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Unity.AI.Models;
using Unity.AI.Responses;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

internal class OpenAIResponseParser : IOpenAIResponseParser, ITransientDependency
{
    public ApplicationAnalysisResponse ParseApplicationAnalysisResponse(string raw)
    {
        var response = new ApplicationAnalysisResponse();
        if (!TryParseJsonObjectFromResponse(AddIdsToAnalysisItems(raw), out var root))
        {
            return response;
        }

        if (TryGetStringProperty(root, AIJsonKeys.Rating, out var rating))
        {
            response.Rating = rating;
        }

        if (TryGetStringProperty(root, AIJsonKeys.Comments, out var comments))
        {
            response.Comments = comments;
        }

        if (TryGetArrayProperty(root, AIJsonKeys.Finding, out var findingsArray))
        {
            response.Findings = ParseFindings(findingsArray).ToList();
        }

        if (TryGetObjectProperty(root, AIJsonKeys.Recommendation, out var recommendation))
        {
            response.Recommendation = ParseRecommendation(recommendation);
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
                        outputPropertyName == AIJsonKeys.NextSteps)
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

    public ApplicationScoringResponse ParseApplicationScoringResponse(string raw, IReadOnlyDictionary<string, string>? questionIdAliasMap = null)
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
                             confidenceProp.TryGetInt32(out var parsedConfidence)
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

            var hidden = item.TryGetProperty(AIJsonKeys.Hidden, out var hiddenProp) &&
                (hiddenProp.ValueKind == JsonValueKind.True || hiddenProp.ValueKind == JsonValueKind.False) &&
                hiddenProp.GetBoolean();
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
                Hidden = hidden,
                Title = title,
                Detail = detail
            };
        }
    }

    private static ApplicationAnalysisRecommendation? ParseRecommendation(JsonElement recommendation)
    {
        string? decision = null;
        if (recommendation.TryGetProperty(AIJsonKeys.Decision, out var decisionProp) &&
            decisionProp.ValueKind == JsonValueKind.String)
        {
            decision = decisionProp.GetString();
        }

        string? rationale = null;
        if (recommendation.TryGetProperty(AIJsonKeys.Rationale, out var rationaleProp) &&
            rationaleProp.ValueKind == JsonValueKind.String)
        {
            rationale = rationaleProp.GetString();
        }

        if (string.IsNullOrWhiteSpace(decision) && string.IsNullOrWhiteSpace(rationale))
        {
            return null;
        }

        return new ApplicationAnalysisRecommendation
        {
            Decision = decision,
            Rationale = rationale
        };
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

    private static bool TryGetObjectProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        value = prop.Clone();
        return true;
    }

    private static int NormalizeConfidence(int confidence)
    {
        var clamped = Math.Clamp(confidence, 0, 100);
        var rounded = (int)Math.Round(clamped / 5.0, MidpointRounding.AwayFromZero) * 5;
        return Math.Clamp(rounded, 0, 100);
    }
}

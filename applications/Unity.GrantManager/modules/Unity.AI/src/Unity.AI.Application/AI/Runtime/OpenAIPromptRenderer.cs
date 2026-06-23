using System;
using System.Collections.Generic;
using System.Text.Json;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIPromptRenderer : ITransientDependency
{
    private const string PromptVersionV0 = "v0";
    private const string PromptVersionV1 = "v1";
    private static readonly Dictionary<string, string> PromptProfiles =
        new(StringComparer.Ordinal)
        {
            [PromptVersionV0] = PromptVersionV0,
            [PromptVersionV1] = PromptVersionV1
        };

    public static string BuildApplicationScoringResponseTemplate(string sectionPayloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(sectionPayloadJson);
            if (!doc.RootElement.TryGetProperty("questions", out var questions) || questions.ValueKind != JsonValueKind.Array)
            {
                return "{}";
            }

            var template = new Dictionary<string, object>();
            foreach (var question in questions.EnumerateArray())
            {
                if (!question.TryGetProperty("id", out var idProp) || idProp.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var questionId = idProp.GetString();
                if (string.IsNullOrWhiteSpace(questionId))
                {
                    continue;
                }

                template[questionId] = new
                {
                    answer = string.Empty,
                    rationale = string.Empty,
                    confidence = 0
                };
            }

            if (template.Count == 0)
            {
                return "{}";
            }

            return JsonSerializer.Serialize(template, AIJsonDefaults.Indented);
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    public static string BuildAliasedApplicationScoringSection(string? sectionName, string sectionJson, out IReadOnlyDictionary<string, string> questionIdAliasMap)
    {
        questionIdAliasMap = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(sectionJson))
        {
            return JsonSerializer.Serialize(new { name = sectionName, questions = sectionJson }, AIJsonDefaults.Indented);
        }

        try
        {
            using var sectionDoc = JsonDocument.Parse(sectionJson);
            if (sectionDoc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return JsonSerializer.Serialize(new { name = sectionName, questions = sectionDoc.RootElement.Clone() }, AIJsonDefaults.Indented);
            }

            var aliasedQuestions = new List<Dictionary<string, object?>>();
            var aliasMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var index = 1;

            foreach (var question in sectionDoc.RootElement.EnumerateArray())
            {
                if (question.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var aliasedQuestion = new Dictionary<string, object?>(StringComparer.Ordinal);
                string? questionAlias = null;

                foreach (var property in question.EnumerateObject())
                {
                    if (property.NameEquals("id") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var originalQuestionId = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(originalQuestionId))
                        {
                            questionAlias = $"q{index++}";
                            aliasMap[questionAlias] = originalQuestionId;
                            aliasedQuestion[property.Name] = questionAlias;
                            continue;
                        }
                    }

                    aliasedQuestion[property.Name] = property.Value.Clone();
                }

                if (!string.IsNullOrWhiteSpace(questionAlias))
                {
                    aliasedQuestions.Add(aliasedQuestion);
                }
            }

            questionIdAliasMap = aliasMap;
            return JsonSerializer.Serialize(new { name = sectionName, questions = aliasedQuestions }, AIJsonDefaults.Indented);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { name = sectionName, questions = sectionJson }, AIJsonDefaults.Indented);
        }
    }

    public static string ResolvePromptVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException("AI prompt version is not configured.");
        }

        if (PromptProfiles.TryGetValue(version.Trim(), out var selectedVersion))
        {
            return selectedVersion;
        }

        throw new InvalidOperationException($"AI prompt version '{version}' is not supported.");
    }

    public static int ResolvePromptVersionNumber(string version)
    {
        var normalizedVersion = ResolvePromptVersion(version);
        if (normalizedVersion.Length < 2 || !int.TryParse(normalizedVersion.AsSpan(1), out var versionNumber))
        {
            throw new InvalidOperationException($"AI prompt version '{version}' is not supported.");
        }

        return versionNumber;
    }
}

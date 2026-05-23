using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class OpenAIPromptRenderer : ITransientDependency
{
    private const string PromptVersionV0 = "v0";
    private const string PromptVersionV1 = "v1";
    private static readonly string PromptTemplatesFolder = Path.Combine("AI", "Prompts", "Versions");
    private const string ApplicationAnalysisSystemTemplateName = "application-analysis.system";
    private const string ApplicationAnalysisUserTemplateName = "application-analysis.user";
    private const string AttachmentSummarySystemTemplateName = "attachment-summary.system";
    private const string AttachmentSummaryUserTemplateName = "attachment-summary.user";
    private const string ApplicationScoringSystemTemplateName = "application-scoring.system";
    private const string ApplicationScoringUserTemplateName = "application-scoring.user";
    private static readonly Dictionary<string, string> PromptProfiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [PromptVersionV0] = PromptVersionV0,
            [PromptVersionV1] = PromptVersionV1
        };
    private static readonly ConcurrentDictionary<string, string> PromptTemplateCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonLogOptions = new() { WriteIndented = true };

    public static string BuildApplicationAnalysisSystemPrompt(string version)
    {
        return GetRequiredPromptTemplate(version, ApplicationAnalysisSystemTemplateName);
    }

    public static string BuildApplicationAnalysisUserPrompt(string version, string schema, string data, string attachments)
    {
        var replacements = new Dictionary<string, string>
        {
            ["SCHEMA"] = schema,
            ["DATA"] = data,
            ["ATTACHMENTS"] = attachments
        };

        return RenderPromptTemplate(version, ApplicationAnalysisUserTemplateName, replacements);
    }

    public static string BuildAttachmentSummarySystemPrompt(string version)
    {
        return GetRequiredPromptTemplate(version, AttachmentSummarySystemTemplateName);
    }

    public static string BuildAttachmentSummaryUserPrompt(string version, string attachment)
    {
        return RenderPromptTemplate(version, AttachmentSummaryUserTemplateName, new Dictionary<string, string>
        {
            ["ATTACHMENT"] = attachment
        });
    }

    public static string BuildApplicationScoringSystemPrompt(string version)
    {
        return GetRequiredPromptTemplate(version, ApplicationScoringSystemTemplateName);
    }

    public static string BuildApplicationScoringUserPrompt(string version, string data, string attachments, string section, string response)
    {
        return RenderPromptTemplate(version, ApplicationScoringUserTemplateName, new Dictionary<string, string>
        {
            ["DATA"] = data,
            ["ATTACHMENTS"] = attachments,
            ["SECTION"] = section,
            ["RESPONSE"] = response
        });
    }

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

            return JsonSerializer.Serialize(template, JsonLogOptions);
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
            return JsonSerializer.Serialize(new { name = sectionName, questions = sectionJson }, JsonLogOptions);
        }

        try
        {
            using var sectionDoc = JsonDocument.Parse(sectionJson);
            if (sectionDoc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return JsonSerializer.Serialize(new { name = sectionName, questions = sectionDoc.RootElement.Clone() }, JsonLogOptions);
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
            return JsonSerializer.Serialize(new { name = sectionName, questions = aliasedQuestions }, JsonLogOptions);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { name = sectionName, questions = sectionJson }, JsonLogOptions);
        }
    }

    public static string ResolvePromptVersion(string? version)
    {
        if (!string.IsNullOrWhiteSpace(version) &&
            PromptProfiles.TryGetValue(version.Trim(), out var selectedVersion))
        {
            return selectedVersion;
        }

        return PromptVersionV1;
    }

    private static bool TryGetPromptTemplate(string version, string templateName, out string template)
    {
        template = string.Empty;
        var cacheKey = $"{version}/{templateName}";
        if (PromptTemplateCache.TryGetValue(cacheKey, out var cachedTemplate))
        {
            template = cachedTemplate;
            return true;
        }

        var path = Path.Combine(AppContext.BaseDirectory, PromptTemplatesFolder, version, $"{templateName}.txt");
        if (!File.Exists(path))
        {
            return false;
        }

        var loaded = PromptTemplateCache.GetOrAdd(cacheKey, _ => File.ReadAllText(path));
        if (string.IsNullOrWhiteSpace(loaded))
        {
            return false;
        }

        template = loaded;
        return true;
    }

    private static string GetRequiredPromptTemplate(string version, string templateName)
    {
        if (TryGetPromptTemplate(version, templateName, out var template))
        {
            return template;
        }

        throw new InvalidOperationException(
            $"Missing required prompt template '{templateName}.txt' for prompt version '{version}'.");
    }

    private static string RenderPromptTemplate(
        string version,
        string templateName,
        IReadOnlyDictionary<string, string> runtimeReplacements)
    {
        return RenderPromptTemplateInternal(
            version,
            templateName,
            runtimeReplacements,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static string RenderPromptTemplateInternal(
        string version,
        string templateName,
        IReadOnlyDictionary<string, string> runtimeReplacements,
        ISet<string> resolutionStack)
    {
        if (!resolutionStack.Add(templateName))
        {
            throw new InvalidOperationException(
                $"Detected cyclic prompt fragment reference while resolving '{templateName}.txt' for prompt version '{version}'.");
        }

        var template = GetRequiredPromptTemplate(version, templateName);
        var replacements = new Dictionary<string, string>(runtimeReplacements, StringComparer.Ordinal);
        var baseTemplateName = GetTemplateBaseName(templateName);

        foreach (var placeholder in GetTemplatePlaceholders(template))
        {
            if (replacements.ContainsKey(placeholder))
            {
                continue;
            }

            var fragmentTemplateName = ResolveFragmentTemplateName(version, baseTemplateName, placeholder);
            if (!string.IsNullOrWhiteSpace(fragmentTemplateName))
            {
                replacements[placeholder] = RenderPromptTemplateInternal(
                    version,
                    fragmentTemplateName,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    resolutionStack).TrimEnd();
            }
        }

        var rendered = template;
        foreach (var replacement in replacements)
        {
            rendered = rendered.Replace($"{{{{{replacement.Key}}}}}", replacement.Value ?? string.Empty, StringComparison.Ordinal);
        }

        var unresolved = GetTemplatePlaceholders(rendered);
        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unresolved prompt placeholders in '{templateName}.txt' for prompt version '{version}': {string.Join(", ", unresolved.OrderBy(item => item))}");
        }

        resolutionStack.Remove(templateName);
        return rendered;
    }

    private static string? ResolveFragmentTemplateName(string version, string baseTemplateName, string placeholderName)
    {
        var normalizedPlaceholder = placeholderName.ToLowerInvariant();
        var baseScopedCandidate = $"{baseTemplateName}.{normalizedPlaceholder}";
        if (TryGetPromptTemplate(version, baseScopedCandidate, out _))
        {
            return baseScopedCandidate;
        }

        if (string.Equals(placeholderName, "RESPONSE", StringComparison.Ordinal))
        {
            var outputCandidate = $"{baseTemplateName}.output";
            if (TryGetPromptTemplate(version, outputCandidate, out _))
            {
                return outputCandidate;
            }
        }

        if (TryResolveCommonTemplateName(placeholderName, out var commonTemplateName) &&
            TryGetPromptTemplate(version, commonTemplateName, out _))
        {
            return commonTemplateName;
        }

        return null;
    }

    private static bool TryResolveCommonTemplateName(string placeholderName, out string commonTemplateName)
    {
        commonTemplateName = string.Empty;
        if (!placeholderName.StartsWith("COMMON_", StringComparison.Ordinal))
        {
            return false;
        }

        var suffix = placeholderName.Substring("COMMON_".Length).ToLowerInvariant();
        suffix = suffix.Replace('_', '.');
        commonTemplateName = $"common.{suffix}";
        return true;
    }

    private static string GetTemplateBaseName(string templateName)
    {
        var separatorIndex = templateName.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return templateName;
        }

        return templateName.Substring(0, separatorIndex);
    }

    private static HashSet<string> GetTemplatePlaceholders(string template)
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        var searchIndex = 0;

        while (searchIndex < template.Length)
        {
            var start = template.IndexOf("{{", searchIndex, StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = template.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            var placeholder = template.Substring(start + 2, end - start - 2).Trim();
            if (!string.IsNullOrWhiteSpace(placeholder))
            {
                placeholders.Add(placeholder);
            }

            searchIndex = end + 2;
        }

        return placeholders;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Unity.AI.Runtime;

public static class AIPromptTemplateRenderer
{
    public static string BuildApplicationAnalysisUserPrompt(
        string userPromptTemplate,
        string schema,
        string data,
        string attachments,
        string? metadataJson = null)
    {
        return RenderPromptTemplate(
            userPromptTemplate,
            metadataJson,
            new Dictionary<string, string>
            {
                ["SCHEMA"] = schema,
                ["DATA"] = data,
                ["ATTACHMENTS"] = attachments
            });
    }

    public static string BuildAttachmentSummaryUserPrompt(
        string userPromptTemplate,
        string attachment,
        string? metadataJson = null)
    {
        return RenderPromptTemplate(
            userPromptTemplate,
            metadataJson,
            new Dictionary<string, string>
            {
                ["ATTACHMENT"] = attachment,
                ["ATTACHMENTS"] = attachment
            });
    }

    public static string BuildApplicationScoringUserPrompt(
        string userPromptTemplate,
        string data,
        string attachments,
        string section,
        string response,
        string? metadataJson = null)
    {
        return RenderPromptTemplate(
            userPromptTemplate,
            metadataJson,
            new Dictionary<string, string>
            {
                ["DATA"] = data,
                ["ATTACHMENTS"] = attachments,
                ["SECTION"] = section,
                ["RESPONSE"] = response
            });
    }

    public static string BuildFormMappingUserPrompt(
        string userPromptTemplate,
        string data,
        string? metadataJson = null)
    {
        return RenderPromptTemplate(
            userPromptTemplate,
            metadataJson,
            new Dictionary<string, string>
            {
                ["DATA"] = data
            });
    }

    private static string RenderPromptTemplate(
        string template,
        string? metadataJson,
        IReadOnlyDictionary<string, string> runtimeReplacements)
    {
        var placeholders = GetTemplatePlaceholders(template);
        var replacements = new Dictionary<string, string>(runtimeReplacements, StringComparer.Ordinal);

        foreach (var (key, value) in ExtractMetadataSections(metadataJson))
        {
            replacements.TryAdd(key, value);
        }

        if (!replacements.ContainsKey("RESPONSE") && replacements.TryGetValue("OUTPUT", out var outputTemplate))
        {
            replacements["RESPONSE"] = outputTemplate;
        }
        else if (!replacements.ContainsKey("OUTPUT") && replacements.TryGetValue("RESPONSE", out var responseTemplate))
        {
            replacements["OUTPUT"] = responseTemplate;
        }

        var unresolved = placeholders
            .Where(placeholder => !replacements.ContainsKey(placeholder))
            .OrderBy(placeholder => placeholder)
            .ToList();
        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException(
                $"Unresolved prompt placeholders: {string.Join(", ", unresolved)}");
        }

        var rendered = template;
        foreach (var placeholder in placeholders)
        {
            rendered = rendered.Replace($"{{{{{placeholder}}}}}", replacements[placeholder] ?? string.Empty, StringComparison.Ordinal);
        }

        return rendered;
    }

    private static Dictionary<string, string> ExtractMetadataSections(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            return ExtractStringProperties(root);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid prompt metadata JSON.", ex);
        }
    }

    private static Dictionary<string, string> ExtractStringProperties(JsonElement element)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                values[property.Name] = property.Value.GetString() ?? string.Empty;
            }
        }

        return values;
    }

    private static HashSet<string> GetTemplatePlaceholders(string template)
    {
        var placeholders = new HashSet<string>(StringComparer.Ordinal);
        var invalidPlaceholders = new List<string>();
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
            if (IsPromptPlaceholder(placeholder))
            {
                placeholders.Add(placeholder);
            }
            else
            {
                invalidPlaceholders.Add(placeholder);
            }

            searchIndex = end + 2;
        }

        if (invalidPlaceholders.Count > 0)
        {
            throw new InvalidOperationException(
                $"Invalid prompt placeholders: {string.Join(", ", invalidPlaceholders.OrderBy(item => item))}");
        }

        return placeholders;
    }

    private static bool IsPromptPlaceholder(string placeholder)
    {
        return !string.IsNullOrWhiteSpace(placeholder) &&
               placeholder.All(character => char.IsUpper(character) || char.IsDigit(character) || character == '_');
    }
}

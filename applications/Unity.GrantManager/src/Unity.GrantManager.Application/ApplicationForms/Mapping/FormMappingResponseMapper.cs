using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.AI.Responses;

namespace Unity.GrantManager.ApplicationForms.Mapping;

internal static class FormMappingResponseMapper
{
    internal static string BuildSubmissionHeaderMapping(FormMappingResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Mapping))
        {
            return "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(response.Mapping);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return "{}";
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(property.Value.GetString())
                    || string.IsNullOrWhiteSpace(property.Name))
                {
                    return "{}";
                }
            }

            return document.RootElement.GetRawText();
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    internal static List<FormMappingDto> ParseSuggestions(string mapping)
    {
        if (string.IsNullOrWhiteSpace(mapping))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(mapping);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return document.RootElement.EnumerateObject()
                .Where(property => property.Value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(property.Name)
                    && !string.IsNullOrWhiteSpace(property.Value.GetString()))
                .Select(property => new FormMappingDto
                {
                    TargetField = property.Name,
                    SourceField = property.Value.GetString() ?? string.Empty
                })
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    internal static string MergeSubmissionHeaderMapping(
        string? existingMapping,
        IEnumerable<FormMappingDto> suggestions)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(existingMapping))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<Dictionary<string, string>>(existingMapping);
                if (existing != null)
                {
                    foreach (var pair in existing.Where(pair =>
                        !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value)))
                    {
                        mapping[pair.Key] = pair.Value;
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        foreach (var suggestion in suggestions)
        {
            if (!string.IsNullOrWhiteSpace(suggestion.TargetField)
                && !string.IsNullOrWhiteSpace(suggestion.SourceField)
                && !mapping.ContainsKey(suggestion.TargetField))
            {
                mapping[suggestion.TargetField] = suggestion.SourceField;
            }
        }

        return JsonSerializer.Serialize(mapping);
    }
}

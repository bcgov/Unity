using System;
using System.Collections.Generic;
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

            var reversed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var unityField = property.Value.GetString();
                if (string.IsNullOrWhiteSpace(unityField))
                {
                    continue;
                }

                reversed[unityField] = property.Name;
            }

            return JsonSerializer.Serialize(reversed);
        }
        catch (JsonException)
        {
            return "{}";
        }
    }
}

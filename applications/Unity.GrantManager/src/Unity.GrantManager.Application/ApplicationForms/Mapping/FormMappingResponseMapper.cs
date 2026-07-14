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
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    return "{}";
                }

                var chefsField = property.Value.GetString();
                if (string.IsNullOrWhiteSpace(chefsField) || string.IsNullOrWhiteSpace(property.Name))
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
}

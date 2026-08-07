using System.Text.Json;

namespace Unity.GrantManager.ApplicationForms.Mapping;

internal static class FormMappingPromptDataBuilder
{
    internal static JsonElement Build(ApplicationFormMappingReadModelDto readModel)
    {
        var existingMapping = ParseExistingMapping(readModel.ExistingMapping);
        var promptData = new
        {
            chefsData = new
            {
                applicationFormId = readModel.ApplicationFormId,
                applicationFormVersionId = readModel.ApplicationFormVersionId,
                chefsApplicationFormGuid = readModel.ChefsApplicationFormGuid,
                chefsFormVersionGuid = readModel.ChefsFormVersionGuid,
                fields = readModel.ChefsFields
            },
            unityData = new
            {
                coreFields = readModel.UnityCoreFields,
                customFields = readModel.Worksheets
            },
            existingMapping
        };

        return JsonSerializer.SerializeToElement(promptData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static JsonElement ParseExistingMapping(string? existingMapping)
    {
        if (string.IsNullOrWhiteSpace(existingMapping))
        {
            return JsonSerializer.SerializeToElement(new { });
        }

        try
        {
            using var document = JsonDocument.Parse(existingMapping);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(existingMapping);
        }
    }
}

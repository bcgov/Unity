using System.Text.Json;

namespace Unity.GrantManager.ApplicationForms.Mapping;

internal static class FormMappingPromptDataBuilder
{
    internal static JsonElement Build(ApplicationFormMappingReadModelDto readModel)
    {
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
            }
        };

        return JsonSerializer.SerializeToElement(promptData, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

using System;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public static class AiWorksheetSuggestionName
{
    public static string Build(Guid formId, Guid formVersionId) =>
        $"ai-form-{formId}-version-{formVersionId}-field-suggestions";
}

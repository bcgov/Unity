using System;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public static class AiWorksheetSuggestionName
{
    public static string Build(Guid formVersionId) =>
        $"ai-{formVersionId:N}-worksheet";
}

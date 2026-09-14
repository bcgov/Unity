using System;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public static class AiScoresheetSuggestionName
{
    public static string Build(Guid formVersionId) =>
        $"ai-{formVersionId:N}-scoresheet";
}

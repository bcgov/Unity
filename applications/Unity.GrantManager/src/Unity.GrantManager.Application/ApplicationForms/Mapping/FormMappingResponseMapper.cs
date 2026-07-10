using System;
using System.Collections.Generic;
using Unity.AI.Responses;

namespace Unity.GrantManager.ApplicationForms.Mapping;

internal static class FormMappingResponseMapper
{
    internal static Dictionary<string, string> BuildSubmissionHeaderMapping(FormMappingResponse response)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in response.CoreFieldMatches)
        {
            AddMapping(mapping, match.SourceField, match.TargetField);
        }

        foreach (var worksheetMatch in response.WorksheetMatches)
        {
            foreach (var match in worksheetMatch.FieldMatches)
            {
                AddMapping(mapping, match.SourceField, match.TargetField);
            }
        }

        return mapping;
    }

    private static void AddMapping(Dictionary<string, string> mapping, string? sourceField, string? targetField)
    {
        if (string.IsNullOrWhiteSpace(sourceField) || string.IsNullOrWhiteSpace(targetField))
        {
            return;
        }

        mapping[sourceField] = targetField;
    }
}

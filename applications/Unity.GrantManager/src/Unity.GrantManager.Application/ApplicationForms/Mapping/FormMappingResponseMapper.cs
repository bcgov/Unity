using System;
using Unity.AI.Responses;

namespace Unity.GrantManager.ApplicationForms.Mapping;

internal static class FormMappingResponseMapper
{
    internal static string BuildSubmissionHeaderMapping(FormMappingResponse response)
    {
        return string.IsNullOrWhiteSpace(response.Mapping)
            ? "{}"
            : response.Mapping;
    }
}

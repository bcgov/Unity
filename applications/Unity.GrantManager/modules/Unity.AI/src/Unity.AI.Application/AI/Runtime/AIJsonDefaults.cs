using System.Text.Json;

namespace Unity.AI.Runtime;

internal static class AIJsonDefaults
{
    internal static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    internal static readonly JsonSerializerOptions IndentedCamelCase = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static AIJsonDefaults()
    {
        Indented.MakeReadOnly(populateMissingResolver: true);
        IndentedCamelCase.MakeReadOnly(populateMissingResolver: true);
    }
}

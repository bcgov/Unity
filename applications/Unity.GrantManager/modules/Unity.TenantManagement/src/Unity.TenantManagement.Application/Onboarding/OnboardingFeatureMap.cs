using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unity.TenantManagement.Onboarding;

internal static class OnboardingFeatureMap
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Display name / delimited-string variants
        ["Payments"]                  = "Unity.Payments",
        ["Notifications"]             = "Unity.Notifications",
        ["Flex"]                      = "Unity.Flex",
        ["Reporting"]                 = "Unity.Reporting",
        ["AI Reporting"]              = "Unity.AIReporting",
        ["AI Attachment Summaries"]   = "Unity.AI.AttachmentSummaries",
        ["AI Application Analysis"]   = "Unity.AI.ApplicationAnalysis",
        ["AI Scoring"]                = "Unity.AI.Scoring",
        ["Analytics"]                 = "Unity.Analytics",
        // camelCase key aliases used by the checkbox-group JSON format
        ["aiReporting"]               = "Unity.AIReporting",
        ["aiAttachmentSummaries"]     = "Unity.AI.AttachmentSummaries",
        ["aiApplicationAnalysis"]     = "Unity.AI.ApplicationAnalysis",
        ["aiScoring"]                 = "Unity.AI.Scoring",
    };

    private static readonly char[] _delimiters = [',', ';', '|'];

    private sealed class CheckboxItem
    {
        [JsonPropertyName("key")]   public string Key   { get; set; } = string.Empty;
        [JsonPropertyName("value")] public bool   Value { get; set; }
    }

    /// <summary>
    /// Accepts either a JSON checkbox-group array or a delimited string (comma / semicolon / pipe)
    /// and returns the ABP feature keys for all enabled/listed recognised names.
    /// </summary>
    public static IReadOnlyList<string> ResolveFeatureKeys(string features)
    {
        if (string.IsNullOrWhiteSpace(features))
            return [];

        IEnumerable<string> tokens;
        var trimmed = features.Trim();

        if (trimmed.StartsWith('['))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<CheckboxItem>>(trimmed);
                tokens = items?.Where(i => i.Value).Select(i => i.Key) ?? [];
            }
            catch
            {
                tokens = [];
            }
        }
        else
        {
            tokens = trimmed.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [.. tokens
            .Select(token => _map.TryGetValue(token, out var key) ? key : null)
            .Where(key => key is not null)
            .Cast<string>()
            .Distinct()];
    }
}

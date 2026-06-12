using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.TenantManagement.Onboarding;

internal static class OnboardingFeatureMap
{
    private static readonly Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Payments"]                  = "Unity.Payments",
        ["Notifications"]             = "Unity.Notifications",
        ["Flex"]                      = "Unity.Flex",
        ["Reporting"]                 = "Unity.Reporting",
        ["AI Reporting"]              = "Unity.AIReporting",
        ["AI Attachment Summaries"]   = "Unity.AI.AttachmentSummaries",
        ["AI Application Analysis"]   = "Unity.AI.ApplicationAnalysis",
        ["AI Scoring"]                = "Unity.AI.Scoring",
        ["Analytics"]                 = "Unity.Analytics",
    };

    /// <summary>
    /// Parses a comma-separated features string and returns the feature keys for any recognised names.
    /// Unrecognised names are silently skipped.
    /// </summary>
    public static IReadOnlyList<string> ResolveFeatureKeys(string features)
    {
        if (string.IsNullOrWhiteSpace(features))
            return [];

        return [.. features
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => _map.TryGetValue(token, out var key) ? key : null)
            .Where(key => key is not null)
            .Cast<string>()
            .Distinct()];
    }
}

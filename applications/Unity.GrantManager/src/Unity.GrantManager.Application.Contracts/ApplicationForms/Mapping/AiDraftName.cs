using System;
using System.Linq;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public static class AiDraftName
{
    public static string NormalizeTitle(string title) =>
        string.Concat(title.Trim()
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant));

    public static string BuildBaseName(string title)
    {
        var normalizedTitle = NormalizeTitle(title);
        return $"ai-{(string.IsNullOrEmpty(normalizedTitle) ? "draft" : normalizedTitle)}";
    }
}

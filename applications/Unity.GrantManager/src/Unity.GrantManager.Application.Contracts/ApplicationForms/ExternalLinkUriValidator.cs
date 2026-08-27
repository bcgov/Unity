using System;

namespace Unity.GrantManager.ApplicationForms;

/// <summary>
/// Shared http/https-only URI validation to block script-scheme injection (e.g. javascript:, data:).
/// </summary>
public static class ExternalLinkUriValidator
{
    public static bool IsValidHttpUri(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        return Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
    }
}

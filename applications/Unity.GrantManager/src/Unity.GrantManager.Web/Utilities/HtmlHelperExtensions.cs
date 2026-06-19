using Ganss.Xss;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Unity.GrantManager.Web.Utilities;

public static class HtmlHelperExtensions
{
    private static readonly HtmlSanitizer _sanitizer = new();

    public static IHtmlContent SanitizeRaw(this IHtmlHelper _, string? value)
    {
        if (string.IsNullOrEmpty(value)) return HtmlString.Empty;
        return new HtmlString(_sanitizer.Sanitize(value));
    }
}

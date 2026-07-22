using Ganss.Xss;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Unity.GrantManager.Web.Utilities;

public static class HtmlHelperExtensions
{
    private static readonly HtmlSanitizer _sanitizer = BuildSanitizer();

    public static IHtmlContent SanitizeRaw(this IHtmlHelper _, string? value)
    {
        if (string.IsNullOrEmpty(value)) return HtmlString.Empty;
        return new HtmlString(_sanitizer.Sanitize(value));
    }

    private static HtmlSanitizer BuildSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(
        [
            "a", "b", "blockquote", "br", "code", "del", "em",
            "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i",
            "li", "ol", "p", "pre", "s", "span", "strong", "u", "ul"
        ]);

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["href", "rel", "target", "title"]);

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);

        sanitizer.AllowedCssProperties.Clear();

        return sanitizer;
    }
}

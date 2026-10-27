using System.Globalization;
using System.Linq;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace Unity.GrantManager.ApplicantPortal;

public static class MultipleIdentitiesMessageContent
{
    public static string Sanitize(string? html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(["p", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a"]);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(["href", "title"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        sanitizer.AllowedCssProperties.Clear();
        return sanitizer.Sanitize(html ?? string.Empty);
    }

    public static bool HasText(string html)
    {
        var text = new HtmlParser().ParseDocument(html).Body?.TextContent ?? string.Empty;
        return text.Any(character => !char.IsWhiteSpace(character)
            && char.GetUnicodeCategory(character) != UnicodeCategory.Format);
    }
}

using Ganss.Xss;
using Markdig;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Renderers;

public partial class MarkdownRenderer : IMarkdownRenderer, ITransientDependency
{
    private readonly HtmlSanitizer _htmlSanitizer;
    protected MarkdownPipeline MarkdownPipeline { get; }

    public MarkdownRenderer(MarkdownPipeline markdownPipeline)
    {
        MarkdownPipeline = markdownPipeline;
        _htmlSanitizer = new HtmlSanitizer();
        _htmlSanitizer.AllowedAttributes.Add("class");
        _htmlSanitizer.AllowedAttributes.Add("data-bs-toggle");
        _htmlSanitizer.AllowedAttributes.Add("data-bs-target");
    }

    public Task<string> RenderAsync(string markdownText, bool preventXSS = true)
    {
        var renderedMarkdownHtml = Markdown.ToHtml(markdownText, MarkdownPipeline);

        if (preventXSS)
        {
            renderedMarkdownHtml = _htmlSanitizer.Sanitize(renderedMarkdownHtml);
        }

        //renderedMarkdownHtml = SetReferralLinks(renderedMarkdownHtml);

        return Task.FromResult(renderedMarkdownHtml);
    }

    private static string SetReferralLinks(string html)
    {
        var regex = AnchorTagRegex();
        return regex.Replace(html, $"<a target=\"_blank\" rel=\"noopener\" $1");
    }

    [GeneratedRegex("<a(.*?>)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.Singleline, "en-CA")]
    private static partial Regex AnchorTagRegex();
}

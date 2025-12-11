using Ganss.Xss;
using Markdig;
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

    public string Render(string markdownText, bool preventXSS = true)
    {
        var renderedMarkdownHtml = Markdown.ToHtml(markdownText, MarkdownPipeline);

        if (preventXSS)
        {
            renderedMarkdownHtml = _htmlSanitizer.Sanitize(renderedMarkdownHtml);
        }

        return renderedMarkdownHtml;
    }
}

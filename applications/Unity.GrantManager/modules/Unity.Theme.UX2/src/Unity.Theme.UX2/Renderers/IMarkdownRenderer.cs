namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Renderers;

public interface IMarkdownRenderer
{
    string Render(string markdownText, bool preventXSS = true);
}

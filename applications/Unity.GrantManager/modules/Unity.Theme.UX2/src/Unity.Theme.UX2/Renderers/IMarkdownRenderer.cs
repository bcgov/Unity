using System.Threading.Tasks;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Renderers;

public interface IMarkdownRenderer
{
    Task<string> RenderAsync(string markdownText, bool preventXSS = true);
}

namespace Unity.GrantManager.Web.Pages;

public class PageSection
{

    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? UrlTarget { get; set; }
    public string? Description { get; set; }
}


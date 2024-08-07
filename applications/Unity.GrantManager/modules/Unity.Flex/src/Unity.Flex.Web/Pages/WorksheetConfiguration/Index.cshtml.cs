using Microsoft.Extensions.Configuration;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class IndexModel : FlexPageModel
{
    public string MaxFileSize { get; set; }
    public IndexModel(IConfiguration configuration)
    {
        MaxFileSize = configuration["S3:MaxFileSize"] ?? "";
    }
    public void OnGet()
    {
        // Method intentionally left empty.
    }
}

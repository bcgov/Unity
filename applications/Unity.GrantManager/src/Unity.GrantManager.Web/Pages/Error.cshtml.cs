using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages;

public class ErrorModel : PageModel
{
    public int HttpStatusCode { get; private set; }
    public string? ApplicationTenantName { get; private set; }
    public string? CurrentTenantName { get; private set; }

    public void OnGet(int httpStatusCode = 0, string? applicationTenantName = null, string? currentTenantName = null)
    {
        HttpStatusCode = httpStatusCode;//HTTP Status Code
        ApplicationTenantName = applicationTenantName;
        CurrentTenantName = currentTenantName;
    }
}

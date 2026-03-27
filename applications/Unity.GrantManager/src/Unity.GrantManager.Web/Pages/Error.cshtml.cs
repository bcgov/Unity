using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages;

public class ErrorModel : PageModel
{
    public int HttpStatusCode { get; private set; }

    public void OnGet(int httpStatusCode = 0)
    {
        HttpStatusCode = httpStatusCode;//HTTP Status Code
    }
}

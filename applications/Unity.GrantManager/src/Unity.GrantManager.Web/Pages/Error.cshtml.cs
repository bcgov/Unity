using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages;

public class ErrorModel : PageModel
{
    public int StatusCode { get; private set; }

    public void OnGet(int httpStatusCode = 0)
    {
        StatusCode = httpStatusCode;
    }
}

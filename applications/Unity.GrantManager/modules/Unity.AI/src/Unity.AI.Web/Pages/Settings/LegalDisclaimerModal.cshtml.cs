using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.AI.Web.Pages.Settings;

public class LegalDisclaimerModalModel : AbpPageModel
{
    public IActionResult OnPost()
    {
        return NoContent();
    }
}

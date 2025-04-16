using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Payments;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.AccountCoding;

public class CreateModalModel(IAccountCodingAppService accountCodingAppService) : AbpPageModel
{
    [BindProperty]
    public CreateUpdateAccountCodingDto? AccountCoding { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        await accountCodingAppService.CreateAsync(AccountCoding!);
        return NoContent();
    }
}



using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Unity.GrantManager.Payments;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.AccountCoding;

public class CreateModalModel(IAccountCodingAppService accountCodingAppService) : AbpPageModel
{
    [BindProperty]
    public CreateUpdateAccountCodingDto? AccountCoding { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await accountCodingAppService.CreateAsync(AccountCoding!);
            return NoContent();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true)
        {
            throw new UserFriendlyException("This Account Coding already exists");
        }
    }
}

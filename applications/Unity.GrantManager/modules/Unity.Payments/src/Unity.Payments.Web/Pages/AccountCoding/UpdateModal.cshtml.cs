using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Payments;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.AccountCoding;

public class UpdateModalModel(IAccountCodingAppService accountCodingAppService) : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateAccountCodingDto? AccountCoding { get; set; }


    public async Task OnGetAsync()
    {
        var accountCodingDto = await accountCodingAppService.GetAsync(Id);
        AccountCoding = ObjectMapper.Map<AccountCodingDto, CreateUpdateAccountCodingDto>(accountCodingDto);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await accountCodingAppService.UpdateAsync(Id, AccountCoding!);
        return NoContent();
    }
}

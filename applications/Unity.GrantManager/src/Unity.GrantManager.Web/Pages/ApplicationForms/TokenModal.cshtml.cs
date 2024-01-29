using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;

namespace Unity.GrantManager.Web.Pages.ApplicationForms
{
    public class TokenModalModel : GrantManagerPageModel
    {
        [BindProperty]
        public string? Token { get; set; } = null;

        private readonly IApplicationFormTokenAppService _applicationFormTokenAppService;

        public TokenModalModel(IApplicationFormTokenAppService applicationFormTokenAppService)
        {
            _applicationFormTokenAppService = applicationFormTokenAppService;
        }

        public async Task OnGetAsync()
        {
            Token = await _applicationFormTokenAppService.GetFormApiTokenAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _applicationFormTokenAppService.SetFormApiTokenAsync(Token);
            return NoContent();
        }
    }
}

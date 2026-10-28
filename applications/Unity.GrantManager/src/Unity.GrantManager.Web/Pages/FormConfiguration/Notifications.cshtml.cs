using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager.Web.Pages.FormConfiguration
{
    public class NotificationsModel(ILogger<NotificationsModel> logger, IApplicationFormAppService applicationFormAppService) : PageModel
    {
        private readonly ILogger<NotificationsModel> _logger = logger;
        public required string FormId { get; set; }

        public async Task<IActionResult> OnGetAsync(string formId)
        {
            if (!Guid.TryParse(formId, out var id))
            {
                return BadRequest();
            }

            var form = await applicationFormAppService.GetAsync(id);
            if (form.IsHiddenFromList)
            {
                return NotFound();
            }

            FormId = formId;
            return Page();
        }
    }
}

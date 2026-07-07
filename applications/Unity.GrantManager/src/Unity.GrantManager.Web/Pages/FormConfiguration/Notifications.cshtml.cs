using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Unity.GrantManager.Web.Pages.FormConfiguration
{
    public class NotificationsModel(ILogger<NotificationsModel> logger) : PageModel
    {
        private readonly ILogger<NotificationsModel> _logger = logger;
        public required string FormId { get; set; }

        public void OnGet(string formId)
        {
            FormId = formId;
        }
    }
}

using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Payments
{
    public class DisplayPaymentHistoryModel() : AbpPageModel
    {
        [BindProperty]
        public string EntityId { get; set; } = string.Empty;

        public ActionResult OnGet(Guid paymentId)
        {
            EntityId = paymentId.ToString();
            return Page();
        }
    }
}
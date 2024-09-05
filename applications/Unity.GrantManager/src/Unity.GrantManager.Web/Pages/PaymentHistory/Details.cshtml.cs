using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Payments
{
    public class DisplayPaymentHistoryModel() : AbpPageModel
    {
        public ActionResult OnGet(Guid paymentId)
        {
            return Page();
        }
    }
}
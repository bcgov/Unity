using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.Payments
{
    public class PaymentsModel : AbpPageModel
    {

        [BindProperty(SupportsGet = true)]
        public Guid BatchPaymentId { get; set; }
    }
}

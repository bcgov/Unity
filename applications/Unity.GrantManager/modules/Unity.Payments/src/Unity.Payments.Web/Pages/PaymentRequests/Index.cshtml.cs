using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Payments.PaymentRequests;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.Payments
{
    public class PaymentsPageModel(IPaymentRequestAppService paymentRequestAppService) : AbpPageModel
    {

        [BindProperty] public decimal? UserPaymentThreshold { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid BatchPaymentId { get; set; }

        public async Task OnGetAsync()
        {
            UserPaymentThreshold = await paymentRequestAppService.GetUserPaymentThresholdAsync();
        }
    }
}

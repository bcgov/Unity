using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.Payments.Domain.PaymentConfigurations;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Unity.Payments.Web.Pages.PaymentConfifurations
{
    public class PaymentConfigurationModel(IPaymentConfigurationRepository paymentConfigurationRepository) : AbpPageModel
    {
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid? AccountCodingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PaymentIdPrefix { get; set; }

        public async Task OnGetAsync()
        {
            // There should be only one payment configuration, so we can use FirstOrDefaultAsync
            PaymentConfiguration? paymentConfiguration = await paymentConfigurationRepository.FirstOrDefaultAsync();          

            if (paymentConfiguration != null)
            {
                AccountCodingId = paymentConfiguration.DefaultAccountCodingId;
                PaymentIdPrefix = paymentConfiguration.PaymentIdPrefix;
            }
        }
    }
}
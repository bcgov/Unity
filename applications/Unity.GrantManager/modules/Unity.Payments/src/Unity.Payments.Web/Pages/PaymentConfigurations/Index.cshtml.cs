using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Unity.Payments.Domain.PaymentConfigurations;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

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
            var paymentConfigurations = await paymentConfigurationRepository.GetListAsync();
            var paymentConfiguration = paymentConfigurations.Count > 0 ? paymentConfigurations[0] : null;

            if (paymentConfiguration != null)
            {
                AccountCodingId = paymentConfiguration.DefaultAccountCodingId;
                PaymentIdPrefix = paymentConfiguration.PaymentIdPrefix;
            }        
        }        
    }
}
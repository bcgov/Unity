using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Unity.Payments.PaymentConfigurations;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.PaymentConfigurations
{
    public class CreatePaymentConfigurationModel : AbpPageModel
    {
        protected IPaymentConfigurationAppService PaymentConfigurationService { get; }

        [BindProperty]
        public PaymentConfigurationViewModel PaymentConfiguration { get; set; } = new();

        [BindProperty]
        public decimal PaymentThreshold { get; set; }

        [BindProperty]
        public string PaymentIdPrefix { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;


        public CreatePaymentConfigurationModel(IPaymentConfigurationAppService iPaymentConfigurationService)
        {
            PaymentConfigurationService = iPaymentConfigurationService;
        }

        public async Task OnGetAsync()
        {
            // Grab the current Payent Configuration
            var paymentConfigurationDto = await PaymentConfigurationService.GetAsync();

            if (paymentConfigurationDto != null)
            {
                PaymentThreshold = paymentConfigurationDto.PaymentThreshold;
                PaymentIdPrefix = paymentConfigurationDto.PaymentIdPrefix;
                PaymentConfiguration.MinistryClient = paymentConfigurationDto.MinistryClient;
                PaymentConfiguration.Responsibility = paymentConfigurationDto.Responsibility;
                PaymentConfiguration.Stob = paymentConfigurationDto.Stob;
                PaymentConfiguration.ServiceLine = paymentConfigurationDto.ServiceLine;
                PaymentConfiguration.ProjectNumber = paymentConfigurationDto.ProjectNumber;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid && PaymentConfiguration != null)
            {
                try
                {
                    await UpsertPaymentConfigurationAsync();
                    StatusMessage = "Successfully Saved Payment Settings.";
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, message: "Exception in CreatePaymentConfigurationModel OnPostAsync");
                    StatusMessage = "An error occurred while saving Payment Settings.";
                }
            }

            return Page();
        }

        private async Task UpsertPaymentConfigurationAsync()
        {
            var existing = await PaymentConfigurationService.GetAsync();

            if (existing == null)
            {
                await PaymentConfigurationService.CreateAsync(new CreatePaymentConfigurationDto
                {
                    PaymentThreshold = PaymentThreshold,
                    PaymentIdPrefix = PaymentIdPrefix,
                    MinistryClient = PaymentConfiguration.MinistryClient,
                    Responsibility = PaymentConfiguration.Responsibility,
                    Stob = PaymentConfiguration.Stob,
                    ServiceLine = PaymentConfiguration.ServiceLine,
                    ProjectNumber = PaymentConfiguration.ProjectNumber
                });
            }
            else
            {
                await PaymentConfigurationService.UpdateAsync(new UpdatePaymentConfigurationDto
                {
                    PaymentThreshold = PaymentThreshold,
                    PaymentIdPrefix = PaymentIdPrefix,
                    MinistryClient = PaymentConfiguration.MinistryClient,
                    Responsibility = PaymentConfiguration.Responsibility,
                    Stob = PaymentConfiguration.Stob,
                    ServiceLine = PaymentConfiguration.ServiceLine,
                    ProjectNumber = PaymentConfiguration.ProjectNumber
                });
            }
        }
    }
}


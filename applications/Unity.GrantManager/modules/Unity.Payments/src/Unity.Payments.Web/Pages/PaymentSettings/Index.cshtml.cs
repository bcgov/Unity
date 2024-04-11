using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Unity.Payments.PaymentSettings;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;


namespace Unity.Payments.Web.Pages.PaymentSettings
{

    public class CreatePaymentSettingsModel : AbpPageModel
    {

        protected IPaymentSettingsAppService PaymentsAppService { get; }

        [BindProperty]
        public PaymentSettingsViewModel? PaymentSettings { get; set; }

        [BindProperty]
        public decimal? PaymentThreshold { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;


        public CreatePaymentSettingsModel(IPaymentSettingsAppService iPaymentsSettingsAppService)
        {
            PaymentsAppService = iPaymentsSettingsAppService;
        }

        public void OnGet()
        {
            PaymentSettings = new();
            // Grab the current Setings from Payment Settings
            var paymentSettingsDto = PaymentsAppService.Get();
            if(paymentSettingsDto != null) {
              PaymentThreshold = paymentSettingsDto.PaymentThreshold;
              PaymentSettings.MinistryClient = paymentSettingsDto.MinistryClient;
              PaymentSettings.Responsibility = paymentSettingsDto.Responsibility;
              PaymentSettings.Stob = paymentSettingsDto.Stob;
              PaymentSettings.ServiceLine = paymentSettingsDto.ServiceLine;
              PaymentSettings.ProjectNumber = paymentSettingsDto.ProjectNumber;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid && PaymentSettings != null)
            {
                try
                {
                    var paymentSettingsDto = new PaymentSettingsDto();
                    paymentSettingsDto.PaymentThreshold = PaymentThreshold;
                    paymentSettingsDto.MinistryClient = PaymentSettings.MinistryClient;
                    paymentSettingsDto.Responsibility = PaymentSettings.Responsibility;
                    paymentSettingsDto.Stob = PaymentSettings.Stob;
                    paymentSettingsDto.ServiceLine = PaymentSettings.ServiceLine;
                    paymentSettingsDto.Stob = PaymentSettings.Stob;
                    paymentSettingsDto.ProjectNumber = PaymentSettings.ProjectNumber;

                    await PaymentsAppService.CreateOrUpdatePaymentSettingsAsync(paymentSettingsDto);
                    StatusMessage = "Successfully Saved Payment Settings.";
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, message: "Exception in CreatePaymentSettingsModel OnPostAsync");
                    StatusMessage = "An error occurred while saving Payment Settings.";                    
                }
            }

            return Page();
        }
    }
}


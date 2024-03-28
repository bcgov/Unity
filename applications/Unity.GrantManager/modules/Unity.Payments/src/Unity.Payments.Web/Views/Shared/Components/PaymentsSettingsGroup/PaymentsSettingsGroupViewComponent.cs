using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.GrantManager.Attributes;
using Unity.Payments.Settings;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments.Web.Views.Shared.Components.PaymentsSettingsGroup
{
    public class PaymentsSettingsGroupViewComponent : AbpViewComponent
    {
        protected IPaymentsSettingsAppService PaymentsAppService { get; }

        public PaymentsSettingsGroupViewComponent(IPaymentsSettingsAppService paymentsAppService)
        {
            PaymentsAppService = paymentsAppService;
        }

        public virtual async Task<IViewComponentResult> InvokeAsync()
        {
            var paymentsSettings = await PaymentsAppService.GetAsync();
            var model = ObjectMapper.Map<PaymentsSettingsDto, UpdatePaymentsSettingsViewModel>(paymentsSettings);

            return View("~/Views/Shared/Components/PaymentsSettingsGroup/Default.cshtml", model);
        }
    }

    public class UpdatePaymentsSettingsViewModel
    {
        [MaxValue(99999999999.99)]
        [Required]
        [Display(Name = "PaymentThreshold")]
        public decimal PaymentThreshold { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Payments;
using Unity.Payments.PaymentThresholds;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.PaymentThresholds;

public class UpdateModalModel(IPaymentThresholdAppService paymentThresholdAppService) : AbpPageModel
{
    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [HiddenInput]
    [BindProperty(SupportsGet = true)]
    public string? UserName { get; set; }    

    [BindProperty]
    public UpdatePaymentThresholdDto? PaymentThreshold { get; set; }


    public async Task OnGetAsync()
    {
        var paymentThresholdDto = await paymentThresholdAppService.GetAsync(Id);
        PaymentThreshold = ObjectMapper.Map<PaymentThresholdDto, UpdatePaymentThresholdDto>(paymentThresholdDto);
        PaymentThreshold.UserName = UserName;
    }

    public async Task<IActionResult> OnPostAsync()
    {


        await paymentThresholdAppService.UpdateAsync(Id, PaymentThreshold!);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.BatchPaymentRequests;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Payment;

#pragma warning disable S125 // Sections of code should not be commented out
public class CreateApplicationPaymentRequestModal : AbpPageModel
{
    [BindProperty]
    public List<ApplicationPaymentRequestModalViewModel>? ApplicationPaymentRequestForm { get; set; } = new List<ApplicationPaymentRequestModalViewModel>();
    public List<Guid> SelectedApplicationIds { get; set; }

    private readonly GrantApplicationAppService _applicationService;
    private readonly IBatchPaymentRequestAppService _batchPaymentRequestService;

    public CreateApplicationPaymentRequestModal(GrantApplicationAppService applicationService,
        IBatchPaymentRequestAppService batchPaymentRequestService)
    {
        SelectedApplicationIds = new List<Guid>();
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _batchPaymentRequestService = batchPaymentRequestService;
    }

    public async void OnGet(string applicationIds)
    {
        SelectedApplicationIds = JsonConvert.DeserializeObject<List<Guid>>(applicationIds) ?? new List<Guid>();
        var applications = await _applicationService.GetApplicationListAsync(SelectedApplicationIds);

        foreach (var application in applications)
        {
            ApplicationPaymentRequestModalViewModel request = new()
            {
                ApplicationId = application.Id,
                ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName,
                Amount = 0,
                Description = "",
                InvoiceNumber = application.ReferenceNo,
            };

            ApplicationPaymentRequestForm!.Add(request);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ApplicationPaymentRequestForm == null) return NoContent();

        await _batchPaymentRequestService.CreateAsync(new CreateBatchPaymentRequestDto()
        {
            Description = "Description",
            PaymentRequests = MapPaymentRequests(),
            Provider = "A"
        });

        return NoContent();
    }

    private List<CreatePaymentRequestDto> MapPaymentRequests()
    {
        var payments = new List<CreatePaymentRequestDto>();

        if (ApplicationPaymentRequestForm == null) return payments;

        foreach (var payment in ApplicationPaymentRequestForm)
        {
            payments.Add(new CreatePaymentRequestDto()
            {
                Amount = payment.Amount,
                CorrelationId = payment.ApplicationId,
                Description = payment.Description,
                InvoiceNumber = payment.InvoiceNumber
            });
        }

        return payments;
    }
}
#pragma warning restore S125 // Sections of code should not be commented out
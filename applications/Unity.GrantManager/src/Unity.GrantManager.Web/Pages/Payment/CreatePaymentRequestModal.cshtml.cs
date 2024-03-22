using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Payment;

#pragma warning disable S125 // Sections of code should not be commented out
public class CreateApplicationPaymentRequestModal : AbpPageModel
{
    [BindProperty]
    public List<ApplicationPaymentRequestModalViewModel>? ApplicationPaymentRequestForm { get; set; } = new List<ApplicationPaymentRequestModalViewModel>();
    public List<Guid> SelectedApplicationIds { get; set; }

    private readonly GrantApplicationAppService _applicationService;

    // private readonly IApplicationPaymentRequestService _applicationApplicationPaymentRequestService;

    public CreateApplicationPaymentRequestModal(GrantApplicationAppService applicationService)
    {
        // _applicationApplicationPaymentRequestService = applicationApplicationPaymentRequestService;
        SelectedApplicationIds = new List<Guid>();
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
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
                InvoiceNumber = application.ReferenceNo
            };

            ApplicationPaymentRequestForm!.Add(request);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine(ApplicationPaymentRequestForm);
        // ApplicationPaymentRequestDto createDto = ObjectMapper.Map<ApplicationPaymentRequestModalViewModel, ApplicationPaymentRequestDto>(ApplicationPaymentRequestForm!);
        // await _applicationApplicationPaymentRequestService.CreateAsync(createDto);
        await Task.CompletedTask;
        return NoContent();
    }
}
#pragma warning restore S125 // Sections of code should not be commented out
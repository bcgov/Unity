using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Identity;
using static Volo.Abp.Identity.Settings.IdentitySettingNames;

namespace Unity.GrantManager.Web.Pages.Payment;

public class CreateApplicationPaymentRequestModal : AbpPageModel
{
    [BindProperty]
    public List<ApplicationPaymentRequestModalViewModel>? ApplicationPaymentRequestForm { get; set; } = new List<ApplicationPaymentRequestModalViewModel>();
    public List<Guid> SelectedApplicationIds { get; set; }

    private readonly GrantApplicationAppService _applicationService;

    //private readonly IApplicationPaymentRequestService _applicationApplicationPaymentRequestService;

    public CreateApplicationPaymentRequestModal(GrantApplicationAppService applicationService)
    {
        //_applicationApplicationPaymentRequestService = applicationApplicationPaymentRequestService;
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    }
    public async void OnGet(string applicationIds)
    {

        SelectedApplicationIds = JsonConvert.DeserializeObject<List<Guid>>(applicationIds) ?? new List<Guid>();
        var applications = await _applicationService.GetApplicationListAsync(SelectedApplicationIds);

        foreach (var application in applications)
        {
            ApplicationPaymentRequestModalViewModel request = new ApplicationPaymentRequestModalViewModel();
            request.ApplicationId = application.Id;
            request.ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName;
            request.Amount = 0;
            request.Description = "";
            request.InvoiceNumber = application.ReferenceNo;
            ApplicationPaymentRequestForm.Add(request);
        }

    }

    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine(ApplicationPaymentRequestForm);
        //ApplicationPaymentRequestDto createDto = ObjectMapper.Map<ApplicationPaymentRequestModalViewModel, ApplicationPaymentRequestDto>(ApplicationPaymentRequestForm!);
        //await _applicationApplicationPaymentRequestService.CreateAsync(createDto);
        return NoContent();
    }
}

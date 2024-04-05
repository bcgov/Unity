using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.BatchPaymentRequests;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.SiteInfo.SiteInfoModal;

public class SiteInfoModalModel : AbpPageModel
{
    [BindProperty]
    public Guid SiteId { get; set; }

    [BindProperty]
    public string ActionType { get; set; } = string.Empty;


    private readonly GrantApplicationAppService _applicationService;
    private readonly IBatchPaymentRequestAppService _batchPaymentRequestService;

    public SiteInfoModalModel()
    {
    }

    public async Task OnGetAsync(Guid siteId, string actionType, string submitButtonText)
    {
        SiteId = siteId;
        ActionType = actionType;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        throw new NotSupportedException();
    }
}
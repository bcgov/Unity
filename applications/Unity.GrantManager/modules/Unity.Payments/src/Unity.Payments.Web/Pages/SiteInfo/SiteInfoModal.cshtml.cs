using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.Validation;
using Unity.Payments.Suppliers;
using Unity.Payments.Enums;

namespace Unity.Payments.Web.SiteInfo.SiteInfoModal;

public class SiteInfoModalModel : AbpPageModel
{
    public List<SelectListItem> PayGroupOptionsList { get; set; }
    [BindProperty]
    public SiteInfoModalModelModel Site { get; set; } = new();

    private readonly ISupplierAppService _supplierService;
    private readonly ISiteAppService _siteAppService;

    public SiteInfoModalModel(ISupplierAppService supplierService, ISiteAppService siteAppService)
    {
        _supplierService = supplierService;
        _siteAppService = siteAppService;

        PayGroupOptionsList =
        new List<SelectListItem>()
        {
            new SelectListItem { Value = ((int)PaymentGroup.Cheque).ToString(), Text = PaymentGroup.Cheque.ToString() },
            new SelectListItem { Value = ((int)PaymentGroup.EFT).ToString(), Text = PaymentGroup.EFT.ToString() },
        };
    }

    public class SiteInfoModalModelModel
    {
        public Guid Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string SupplierNumber { get; set; } = string.Empty;
        public Guid ApplicantId { get; set; }
        public Guid SupplierId { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:SiteNumber")]
        [MaxLength(15, ErrorMessage = "Must be a maximum of 15 characters")]
        public string SiteNumber { get; set; } = string.Empty;
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:PayGroup")]
        [SelectItems(nameof(PayGroupOptionsList))]
        public string PayGroup { get; set; } = string.Empty;
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:AddressLine1")]
        public string? AddressLine1 { get; set; }
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:AddressLine2")]
        public string? AddressLine2 { get; set; }
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:AddressLine3")]
        public string? AddressLine3 { get; set; }
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:City")]
        public string? City { get; set; }
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:Province")]
        public string? Province { get; set; }
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:PostalCode")]
        public string? PostalCode { get; set; }
        
        [Display(Name = "Status")]
        [ReadOnlyInput]
        [DisabledInput]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Email")]
        [ReadOnlyInput]
        [DisabledInput]
        public string Email { get; set; } = string.Empty;
    }


    public async Task OnGetAsync(Guid siteId,
        string actionType,
        string supplierNumber,
        Guid applicantId,
        Guid supplierId)
    {
        Site.Id = siteId;
        Site.ActionType = actionType;
        Site.SupplierNumber = supplierNumber;
        Site.ApplicantId = applicantId;
        Site.SupplierId = supplierId;

        if (Site.ActionType.Contains("Edit"))
        {
            SiteDto site = await _siteAppService.GetAsync(siteId);

            Site.SiteNumber = site.Number ?? "";
            Site.PayGroup = ((int)site.PaymentGroup).ToString();
            Site.AddressLine1 = site.AddressLine1 ?? "";
            Site.AddressLine2 = site.AddressLine2 ?? "";
            Site.AddressLine3 = site.AddressLine3 ?? "";
            Site.City = site.City ?? "";
            Site.Province = site.Province ?? "";
            Site.PostalCode = site.PostalCode ?? "";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Site.ActionType.StartsWith("Edit"))
        {
            await EditSite();

            return NoContent();
        }
        else if (Site.ActionType.StartsWith("Add"))
        {
            await CreateSite();

            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }

    private async Task CreateSite()
    {
        _ = int.TryParse(Site.PayGroup, out int payGroup);
        _ = await _supplierService.CreateSiteAsync(Site.SupplierId, new CreateSiteDto()  // how to get guid
        {
            AddressLine1 = Site.AddressLine1,
            AddressLine2 = Site.AddressLine2,
            AddressLine3 = Site.AddressLine3,
            City = Site.City,
            Number = Site.SiteNumber,
            PaymentGroup = (PaymentGroup)payGroup,
            PostalCode = Site.PostalCode,
            Province = Site.Province
        });
    }

    private async Task EditSite()
    {
        _ = int.TryParse(Site.PayGroup, out int payGroup);
        _ = await _supplierService.UpdateSiteAsync(Site.SupplierId, Site.Id, new UpdateSiteDto()
        {
            AddressLine1 = Site.AddressLine1,
            AddressLine2 = Site.AddressLine2,
            AddressLine3 = Site.AddressLine3,
            City = Site.City,
            Number = Site.SiteNumber,
            PaymentGroup = (PaymentGroup)payGroup,
            PostalCode = Site.PostalCode,
            Province = Site.Province
        });
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.Validation;
using Unity.Payments.SupplierInfo;
using Unity.Payments.Enums;

namespace Unity.Payments.Web.SiteInfo.SiteInfoModal;

public class SiteInfoModalModel : AbpPageModel
{
    public List<SelectListItem> PayGroupOptionsList { get; set; }
    [BindProperty]
    public SiteInfoModalModelModel SiteInfo { get; set; } = new();

    private readonly SupplierInfoAppService _supplierService;

    public SiteInfoModalModel(SupplierInfoAppService supplierService)
    {
        _supplierService = supplierService;
        PayGroupOptionsList = new();
        PayGroupOptionsList.Add(new SelectListItem { Value = ((int)Enums.PaymentGroup.Cheque).ToString(), Text = Enums.PaymentGroup.Cheque.ToString() });
        PayGroupOptionsList.Add(new SelectListItem { Value = ((int)Enums.PaymentGroup.EFT).ToString(), Text = Enums.PaymentGroup.EFT.ToString() });
    }

    public class SiteInfoModalModelModel 
    {
        public Guid SiteId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string SupplierNumber {  get; set; } = string.Empty;
        public Guid ApplicantId {  get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.SiteInfo:SiteNumber")]
        [MaxLength(21, ErrorMessage = "Must be a maximum of 21 characters")]
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
    }


    public async Task OnGetAsync(Guid siteId, string actionType, string supplierNumber, Guid applicantId)
    {
        SiteInfo.SiteId = siteId;
        SiteInfo.ActionType = actionType;
        SiteInfo.SupplierNumber = supplierNumber;
        SiteInfo.ApplicantId = applicantId;
        if (SiteInfo.ActionType.Contains("Edit"))
        {
            SiteDto site = await _supplierService.GetSiteAsync(applicantId, supplierNumber, siteId);
            SiteInfo.SiteNumber = site.Number ?? "";
            SiteInfo.PayGroup = ((int)Enum.Parse(typeof(PaymentGroup), site.PayGroup??"")).ToString()??"";
            SiteInfo.AddressLine1 = site.AddressLine1 ?? "";
            SiteInfo.AddressLine2 = site.AddressLine2 ?? "";
            SiteInfo.AddressLine3 = site.AddressLine3 ?? "";
            SiteInfo.City = site.City ?? "";
            SiteInfo.Province = site.Province ?? "";
            SiteInfo.PostalCode = site.PostalCode ?? "";
        }
        
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (SiteInfo.ActionType.StartsWith("Edit"))
        {
            int payGroup;
            int.TryParse(SiteInfo.PayGroup, out payGroup);
            await _supplierService.UpdateSiteAsync(SiteInfo.SiteId, SiteInfo.ApplicantId, SiteInfo.SupplierNumber, SiteInfo.SiteNumber, payGroup, SiteInfo.AddressLine1, SiteInfo.AddressLine2, SiteInfo.AddressLine3, SiteInfo.City, SiteInfo.Province, SiteInfo.PostalCode);
            return NoContent();
        }
        else if (SiteInfo.ActionType.StartsWith("Add"))
        {
            int payGroup;
            int.TryParse(SiteInfo.PayGroup, out payGroup);
            await _supplierService.InsertSiteAsync(SiteInfo.ApplicantId, SiteInfo.SupplierNumber, SiteInfo.SiteNumber, payGroup, SiteInfo.AddressLine1, SiteInfo.AddressLine2, SiteInfo.AddressLine3, SiteInfo.City, SiteInfo.Province, SiteInfo.PostalCode);
            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }
}
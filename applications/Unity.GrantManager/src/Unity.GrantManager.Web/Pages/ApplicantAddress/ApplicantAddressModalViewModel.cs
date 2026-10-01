using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Pages.ApplicantAddress;

public class ApplicantAddressModalViewModel
{
    [HiddenInput]
    public Guid ApplicantId { get; set; }

    [HiddenInput]
    public Guid Id { get; set; }

    [DisplayName("ApplicantAddress:AddressType")]
    [Required]
    public AddressType AddressType { get; set; } = AddressType.PhysicalAddress;

    public List<SelectListItem> AddressTypeOptions { get; set; } = CreateAddressTypeOptions();

    [DisplayName("ApplicantAddress:Street")]
    [StringLength(500)]
    public string? Street { get; set; }

    [DisplayName("ApplicantAddress:Street2")]
    [StringLength(500)]
    public string? Street2 { get; set; }

    [DisplayName("ApplicantAddress:Unit")]
    [StringLength(100)]
    public string? Unit { get; set; }

    [DisplayName("ApplicantAddress:City")]
    [StringLength(200)]
    public string? City { get; set; }

    [DisplayName("ApplicantAddress:Province")]
    [StringLength(200)]
    public string? Province { get; set; }

    [DisplayName("ApplicantAddress:PostalCode")]
    [StringLength(20)]
    public string? PostalCode { get; set; }

    [DisplayName("ApplicantAddress:PrimaryAddress")]
    public bool IsPrimary { get; set; }

    public void EnsureAddressTypeOptions()
    {
        if (AddressTypeOptions is null || AddressTypeOptions.Count == 0)
        {
            AddressTypeOptions = CreateAddressTypeOptions();
        }
    }

    public static List<SelectListItem> CreateAddressTypeOptions()
    {
        return
        [
            new SelectListItem
            {
                Value = nameof(AddressType.PhysicalAddress),
                Text = AddressTypeMapper.ToDisplayName(AddressType.PhysicalAddress)
            },
            new SelectListItem
            {
                Value = nameof(AddressType.MailingAddress),
                Text = AddressTypeMapper.ToDisplayName(AddressType.MailingAddress)
            }
        ];
    }
}

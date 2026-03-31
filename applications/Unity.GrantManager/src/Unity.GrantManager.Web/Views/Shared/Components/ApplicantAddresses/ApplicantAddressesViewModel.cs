using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses
{
    public class ApplicantAddressesViewModel
    {
        public Guid ApplicantId { get; set; }
        public bool CanEditAddress { get; set; }
        public bool CanSave => CanEditAddress && (PrimaryPhysicalAddress.IsEditable || PrimaryMailingAddress.IsEditable);
        public ApplicantPrimaryAddressViewModel PrimaryPhysicalAddress { get; set; } = new();
        public ApplicantPrimaryAddressViewModel PrimaryMailingAddress { get; set; } = new();
        public List<ApplicantAddressItemDto> Addresses { get; set; } = new List<ApplicantAddressItemDto>();
    }

    public class ApplicantPrimaryAddressViewModel
    {
        public Guid Id { get; set; }
        [Display(Name = "Street")]
        public string Street { get; set; } = string.Empty;
        [Display(Name = "Street 2")]
        public string Street2 { get; set; } = string.Empty;
        [Display(Name = "Unit")]
        public string Unit { get; set; } = string.Empty;
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;
        [Display(Name = "Province")]
        public string Province { get; set; } = string.Empty;
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;
        public bool IsEditable => Id != Guid.Empty;
    }

    public class ApplicantAddressItemDto
    {
        public Guid Id { get; set; }
        public string AddressType { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public Guid? ApplicationId { get; set; }
        public string Street { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Postal { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}

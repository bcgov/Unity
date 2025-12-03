using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses
{
    public class ApplicantAddressesViewModel
    {
        public Guid ApplicantId { get; set; }
        public bool CanEditContact { get; set; }
        public bool CanEditAddress { get; set; }
        public bool CanSave => (CanEditContact && PrimaryContact.IsEditable)
            || (CanEditAddress && (PrimaryPhysicalAddress.IsEditable || PrimaryMailingAddress.IsEditable));
        public ApplicantPrimaryContactViewModel PrimaryContact { get; set; } = new();
        public ApplicantPrimaryAddressViewModel PrimaryPhysicalAddress { get; set; } = new();
        public ApplicantPrimaryAddressViewModel PrimaryMailingAddress { get; set; } = new();
        public List<ApplicantContactItemDto> Contacts { get; set; } = new();
        public List<ApplicantAddressItemDto> Addresses { get; set; } = new List<ApplicantAddressItemDto>();
    }

    public class ApplicantPrimaryContactViewModel
    {
        public Guid Id { get; set; }
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;
        [Display(Name = "Business Phone")]
        public string BusinessPhone { get; set; } = string.Empty;
        [Display(Name = "Cell Phone")]
        public string CellPhone { get; set; } = string.Empty;
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        public bool IsEditable => Id != Guid.Empty;
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

    public class ApplicantContactItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreationTime { get; set; }
    }

    public class ApplicantAddressItemDto
    {
        public Guid Id { get; set; }
        public string AddressType { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Postal { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}

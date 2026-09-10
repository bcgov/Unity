using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses
{
    public class ApplicantAddressesViewModel
    {
        public Guid ApplicantId { get; set; }
        public bool CanEditAddresses { get; set; }
        public Guid? ExpectedApplicationId { get; set; }
        public string ExpectedApplicationReferenceNo { get; set; } = string.Empty;
        public bool CanCreateAddresses => CanEditAddresses && ExpectedApplicationId.HasValue && ExpectedApplicationId != Guid.Empty;
        public bool CanEditPrimaryPhysical => CanEditAddresses && (PrimaryPhysicalAddress.Exists || CanCreateAddresses);
        public bool CanEditPrimaryMailing => CanEditAddresses && (PrimaryMailingAddress.Exists || CanCreateAddresses);
        public bool CanSave => CanEditPrimaryPhysical || CanEditPrimaryMailing;
        public ApplicantPrimaryAddressViewModel PrimaryPhysicalAddress { get; set; } = new();
        public ApplicantPrimaryAddressViewModel PrimaryMailingAddress { get; set; } = new();
        public List<ApplicantAddressItemDto> Addresses { get; set; } = new List<ApplicantAddressItemDto>();
    }

    public class ApplicantPrimaryAddressViewModel
    {
        public Guid Id { get; set; }
        // Individual fields are optional; creation validates Street or Street2 as a pair.
        [Display(Name = "Street")]
        public string? Street { get; set; } = string.Empty;
        [Display(Name = "Street 2")]
        public string? Street2 { get; set; } = string.Empty;
        [Display(Name = "Unit")]
        public string? Unit { get; set; } = string.Empty;
        [Display(Name = "City")]
        public string? City { get; set; } = string.Empty;
        [Display(Name = "Province")]
        public string? Province { get; set; } = string.Empty;
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; } = string.Empty;
        public bool Exists => Id != Guid.Empty;
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

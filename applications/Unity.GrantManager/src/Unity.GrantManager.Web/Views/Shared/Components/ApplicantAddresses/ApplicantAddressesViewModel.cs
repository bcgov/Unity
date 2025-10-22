using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantAddresses
{
    public class ApplicantAddressesViewModel
    {
        public Guid ApplicantId { get; set; }
        public List<ApplicantAddressItemDto> Addresses { get; set; } = new List<ApplicantAddressItemDto>();
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

using System;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Applicants;

public class ApplicantAddressSaveResultDto
{
    public ApplicantSavedAddressDto? PrimaryPhysicalAddress { get; set; }
    public ApplicantSavedAddressDto? PrimaryMailingAddress { get; set; }
}

public class ApplicantSavedAddressDto : ApplicantAddressDto
{
    public Guid? ApplicationId { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

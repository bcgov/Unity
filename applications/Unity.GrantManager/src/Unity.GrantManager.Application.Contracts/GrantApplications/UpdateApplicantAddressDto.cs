using System;

namespace Unity.GrantManager.GrantApplications;

public class UpdateApplicantAddressDto
{
    public Guid ApplicantId { get; set; }
    public AddressType AddressType { get; set; }

    public string? Street { get; set; }
    public string? Street2 { get; set; }
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Postal { get; set; }
}

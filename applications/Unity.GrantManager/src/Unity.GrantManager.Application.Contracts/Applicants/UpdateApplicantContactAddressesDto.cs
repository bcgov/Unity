using System;

namespace Unity.GrantManager.Applicants;

public class UpdateApplicantContactAddressesDto
{
    public UpdatePrimaryContactDto? PrimaryContact { get; set; }
    public UpdatePrimaryApplicantAddressDto? PrimaryPhysicalAddress { get; set; }
    public UpdatePrimaryApplicantAddressDto? PrimaryMailingAddress { get; set; }
}

public class UpdatePrimaryContactDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? BusinessPhone { get; set; }
    public string? CellPhone { get; set; }
}

public class UpdatePrimaryApplicantAddressDto
{
    public Guid Id { get; set; }
    public string? Street { get; set; }
    public string? Street2 { get; set; }
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
}

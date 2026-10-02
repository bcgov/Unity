using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.ApplicantProfile.Addresses;

public class UpdateApplicantProfileAddressDto
{
    [Required]
    public AddressType AddressType { get; set; } = AddressType.PhysicalAddress;

    [StringLength(500)]
    public string? Street { get; set; }

    [StringLength(500)]
    public string? Street2 { get; set; }

    [StringLength(100)]
    public string? Unit { get; set; }

    [StringLength(200)]
    public string? City { get; set; }

    [StringLength(200)]
    public string? Province { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    public bool IsPrimary { get; set; }
}

using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicantAddressDto : EntityDto<Guid>
{
    public Guid ApplicantId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string Street2 { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Postal { get; set; }
    public AddressType AddressType { get; set; }
}
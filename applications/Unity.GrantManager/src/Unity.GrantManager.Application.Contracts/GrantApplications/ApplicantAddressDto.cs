using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicantAddressDto : EntityDto<Guid>, IHasCreationTime, IHasModificationTime
{
    public Guid ApplicantId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string Street2 { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Postal { get; set; }
    public AddressType AddressType { get; set; }

    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}
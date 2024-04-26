using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class ApplicationAddressDto : EntityDto<Guid>
{
    public Guid ApplicationId { get; set; }
    public string Street { get; set; } = String.Empty;
    public string? Unit { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Postal { get; set; }
    public string AddressType { get; set; } = String.Empty;

}

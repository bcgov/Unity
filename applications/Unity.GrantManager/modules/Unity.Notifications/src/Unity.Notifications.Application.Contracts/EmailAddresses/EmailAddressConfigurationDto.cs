using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.EmailAddresses;

public class EmailAddressConfigurationDto : EntityDto<Guid>
{
    public string EmailAddress { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool IsInUse { get; set; }
}

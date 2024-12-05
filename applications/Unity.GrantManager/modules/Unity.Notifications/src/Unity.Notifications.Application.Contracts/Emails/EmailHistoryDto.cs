using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.Emails;

[Serializable]
public class EmailHistoryDto : ExtensibleAuditedEntityDto<Guid>
{
    public string Subject { get; set; } = string.Empty;
    public DateTime? SentDateTime { get; set; }
    public string Body { get; set; } = string.Empty;
    public EmailHistoryUserDto? SentBy { get; set; }
}

public class EmailHistoryUserDto : EntityDto<Guid>
{
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}

using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Notifications.Emails;

[Serializable]
public class EmailHistoryDto : ExtensibleAuditedEntityDto<Guid>
{
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Cc { get; set; } = string.Empty;
    public string Bcc { get; set; } = string.Empty;
    public DateTime? SentDateTime { get; set; }
    public string Body { get; set; } = string.Empty;
    public EmailHistoryUserDto? SentBy { get; set; }
    public string TemplateName { get; set; } = string.Empty;
}

public class EmailHistoryUserDto : EntityDto<Guid>
{
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
}

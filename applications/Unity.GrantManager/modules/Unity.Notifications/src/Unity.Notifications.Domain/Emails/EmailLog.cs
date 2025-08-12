using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Emails;
public class EmailLog : AuditedAggregateRoot<Guid>, IMultiTenant
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string CC { get; set; } = string.Empty;
    public string BCC { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int RetryAttempts { get; set; }
    public Guid? ChesMsgId { get; set; }
    public string ChesResponse { get; set; } = string.Empty;
    public string ChesStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SendOnDateTime { get; set; }
    public DateTime? SentDateTime { get; set; }
    public string TemplateName { get; set; } = string.Empty;
}

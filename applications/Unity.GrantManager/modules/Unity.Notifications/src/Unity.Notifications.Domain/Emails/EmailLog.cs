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
    public string FromAddress { get; set; } = "";
    public string ToAddress { get; set; } = "";
    public string CC { get; set; } = "";
    public string BCC { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public string BodyType { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Tag { get; set; } = "";
    public int RetryAttempts { get; set; }
    public Guid? ChesMsgId { get; set; }
    public string ChesResponse { get; set; } = "";
    public string ChesStatus { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? SendOnDateTime { get; set; }
    public DateTime? SentDateTime { get; set; }
    public string TemplateName { get; set; } = "";
}

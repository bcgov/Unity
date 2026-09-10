using System;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicationForms;

public class GenerationReview : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected GenerationReview()
    {
        ReviewData = "{}";
    }

    public GenerationReview(
        Guid id,
        string operation,
        Guid contextId,
        int sequence = 1)
        : base(id)
    {
        Operation = operation;
        ContextId = contextId;
        Sequence = sequence;
        Status = GenerationReviewStatus.Active;
        ReviewData = "{}";
    }

    public string Operation { get; private set; } = null!;
    public Guid ContextId { get; private set; }
    public int Sequence { get; private set; }
    public GenerationReviewStatus Status { get; private set; }
    [Column(TypeName = "jsonb")]
    public string ReviewData { get; private set; }

    public Guid? TenantId { get; set; }

    public void SetReviewData(string reviewData)
    {
        ReviewData = reviewData;
    }

    public void SetStatus(GenerationReviewStatus status)
    {
        Status = status;
    }

    public void Complete()
    {
        Status = GenerationReviewStatus.Completed;
    }

    public void Discard()
    {
        Status = GenerationReviewStatus.Discarded;
    }
}

using System;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicantMergeOperation : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid PrincipalApplicantId { get; private set; }
    public Guid SecondaryApplicantId { get; private set; }
    public ApplicantMergeStatus Status { get; private set; }
    public ApplicantMergeSource Source { get; private set; }
    public string PrincipalStateBefore { get; private set; } = string.Empty;
    public string PrincipalStateAfter { get; private set; } = string.Empty;
    public string SecondaryStateBefore { get; private set; } = string.Empty;
    public string SecondaryStateAfter { get; private set; } = string.Empty;
    public DateTime MergedAt { get; private set; }
    public Guid? MergedById { get; private set; }
    public DateTime? ReversedAt { get; private set; }
    public Guid? ReversedById { get; private set; }
    public string? ReversalReason { get; private set; }
    public int SnapshotVersion { get; private set; }
    public Collection<ApplicantMergeApplicationChange> ApplicationChanges { get; private set; }

    protected ApplicantMergeOperation()
    {
        ApplicationChanges = [];
    }

    public ApplicantMergeOperation(
        Guid id,
        Guid? tenantId,
        Guid principalApplicantId,
        Guid secondaryApplicantId,
        ApplicantMergeSource source,
        string principalStateBefore,
        string principalStateAfter,
        string secondaryStateBefore,
        string secondaryStateAfter,
        DateTime mergedAt,
        Guid? mergedById,
        int snapshotVersion = 1) : base(id)
    {
        if (principalApplicantId == secondaryApplicantId)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeSameApplicant);
        }

        if (!Enum.IsDefined(source) || snapshotVersion < 1)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
        }

        TenantId = tenantId;
        PrincipalApplicantId = principalApplicantId;
        SecondaryApplicantId = secondaryApplicantId;
        Source = source;
        PrincipalStateBefore = Check.NotNullOrWhiteSpace(principalStateBefore, nameof(principalStateBefore));
        PrincipalStateAfter = Check.NotNullOrWhiteSpace(principalStateAfter, nameof(principalStateAfter));
        SecondaryStateBefore = Check.NotNullOrWhiteSpace(secondaryStateBefore, nameof(secondaryStateBefore));
        SecondaryStateAfter = Check.NotNullOrWhiteSpace(secondaryStateAfter, nameof(secondaryStateAfter));
        MergedAt = mergedAt;
        MergedById = mergedById;
        SnapshotVersion = snapshotVersion;
        Status = ApplicantMergeStatus.Completed;
        ApplicationChanges = [];
    }

    public void AddApplicationChange(ApplicantMergeApplicationChange change)
    {
        if (change.ApplicantMergeOperationId != Id)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
        }

        ApplicationChanges.Add(change);
    }

    public void MarkReversed(Guid? reversedById, DateTime reversedAt, string reason)
    {
        if (Status != ApplicantMergeStatus.Completed)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeAlreadyReversed);
        }

        Status = ApplicantMergeStatus.Reversed;
        ReversedById = reversedById;
        ReversedAt = reversedAt;
        ReversalReason = Check.NotNullOrWhiteSpace(reason, nameof(reason), 1000);
    }
}

public class ApplicantMergeApplicationChange : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid ApplicantMergeOperationId { get; private set; }
    public Guid ApplicationId { get; private set; }
    public bool WasTransferred { get; private set; }
    public Guid ApplicantIdBefore { get; private set; }
    public Guid ApplicantIdAfter { get; private set; }
    public Guid? DefaultSiteIdBefore { get; private set; }
    public Guid? DefaultSiteIdAfter { get; private set; }
    public string RelatedRecordsSnapshot { get; private set; } = string.Empty;

    protected ApplicantMergeApplicationChange()
    {
    }

    public ApplicantMergeApplicationChange(
        Guid id,
        Guid? tenantId,
        Guid applicantMergeOperationId,
        Guid applicationId,
        bool wasTransferred,
        Guid applicantIdBefore,
        Guid applicantIdAfter,
        Guid? defaultSiteIdBefore,
        Guid? defaultSiteIdAfter,
        string relatedRecordsSnapshot) : base(id)
    {
        TenantId = tenantId;
        ApplicantMergeOperationId = applicantMergeOperationId;
        ApplicationId = applicationId;
        WasTransferred = wasTransferred;
        ApplicantIdBefore = applicantIdBefore;
        ApplicantIdAfter = applicantIdAfter;
        DefaultSiteIdBefore = defaultSiteIdBefore;
        DefaultSiteIdAfter = defaultSiteIdAfter;
        RelatedRecordsSnapshot = Check.NotNullOrWhiteSpace(relatedRecordsSnapshot, nameof(relatedRecordsSnapshot));
    }
}

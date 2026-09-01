using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicantMergeOperationRepository : IRepository<ApplicantMergeOperation, Guid>
{
    Task<ApplicantMergeOperation> GetWithChangesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<ApplicantMergeOperation>> GetActiveForApplicantAsync(
        Guid applicantId,
        CancellationToken cancellationToken = default);

    Task<bool> HasLaterActiveMergeAsync(
        ApplicantMergeOperation operation,
        CancellationToken cancellationToken = default);
}

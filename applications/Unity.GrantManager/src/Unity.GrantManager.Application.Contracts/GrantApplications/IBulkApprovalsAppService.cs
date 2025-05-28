using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.GrantApplications
{
    public interface IBulkApprovalsAppService
    {
        Task<BulkApprovalResultDto> BulkApproveApplications(List<BulkApprovalDto> batchApplicationsToApprove);
        Task<List<BulkApprovalDto>> GetApplicationsForBulkApproval(Guid[] applicationGuids);
    }
}

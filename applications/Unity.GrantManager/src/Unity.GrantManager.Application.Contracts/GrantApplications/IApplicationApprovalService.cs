using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.GrantApplications
{
    public interface IApplicationApprovalService
    {
        Task<bool> BulkApproveApplications(Guid[] applicationGuids);
        Task<List<ApplicationApprovalDto>> GetApplicationsForBulkApproval(Guid[] applicationGuids);
    }
}

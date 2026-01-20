using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.GrantApplications
{
    public interface IGrantApplicationsSummaryAppService
    {
        Task<GetSummaryDto> GetSummaryAsync(Guid applicationId);
    }
}

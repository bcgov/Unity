using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting
{
    public interface IScoresheetReportingFieldsSyncAppService : IApplicationService
    {
        Task SyncQuestions(Guid? tenantId);
        Task SyncAnswers(Guid? tenantId);
    }
}

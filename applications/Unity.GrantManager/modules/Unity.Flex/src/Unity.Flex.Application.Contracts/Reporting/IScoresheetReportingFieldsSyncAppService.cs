using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting
{
    public interface IScoresheetReportingFieldsSyncAppService : IApplicationService
    {
        Task GenerateQuestions(Guid scoresheetId);
        Task GenerateAnswers(Guid scoresheetInstanceId);
        Task SyncQuestions();
        Task SyncAnswers();
    }
}

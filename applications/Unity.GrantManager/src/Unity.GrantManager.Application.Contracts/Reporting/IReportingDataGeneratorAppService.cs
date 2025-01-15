using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting
{
    public interface IReportingDataGeneratorAppService : IApplicationService
    {
        Task GenerateReportData(Guid submissionId);
    }
}

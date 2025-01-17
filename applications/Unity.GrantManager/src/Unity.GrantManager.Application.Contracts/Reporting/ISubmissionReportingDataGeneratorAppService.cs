using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting
{
    public interface ISubmissionReportingDataGeneratorAppService : IApplicationService
    {
        Task Generate(Guid submissionId);        
    }
}

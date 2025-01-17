using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting
{
    public interface IFormVersionReportingFieldsGeneratorAppService : IApplicationService
    {
        Task Generate(Guid formVersionId);
    }
}

using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting.FieldGenerators
{
    public interface IReportingFieldsGeneratorService : IApplicationService
    {
        Task GenerateAndSetAsync(ApplicationFormVersion applicationFormVersion);
    }
}

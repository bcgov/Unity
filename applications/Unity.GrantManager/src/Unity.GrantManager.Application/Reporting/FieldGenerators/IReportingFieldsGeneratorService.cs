using System.Threading.Tasks;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Reporting.FieldGenerators
{
    public interface IReportingFieldsGeneratorService
    {
        Task GenerateAndSetAsync(ApplicationFormVersion applicationFormVersion);
    }
}

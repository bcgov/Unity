using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicationForms
{
    public interface IApplicationFormConfigurationAppService : IApplicationService
    {
        Task<ApplicationFormsConfigurationDto> GetConfiguration();
    }
}

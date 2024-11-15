using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.SettingManagement;
public interface IApplicationUiSettingsAppService : IApplicationService
{
    Task<ApplicationUiSettingsDto> GetAsync();
    Task UpdateAsync(ApplicationUiSettingsDto input);
}

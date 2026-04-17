using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.AI.Settings;

public interface IAIConfigurationAppService : IApplicationService
{
    Task<AITenantConfigurationDto> GetTenantConfigurationAsync();
    Task UpdateTenantConfigurationAsync(UpdateAITenantConfigurationDto input);
}

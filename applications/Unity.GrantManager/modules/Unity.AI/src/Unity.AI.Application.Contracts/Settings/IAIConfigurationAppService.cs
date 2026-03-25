using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.AI.Settings;

public interface IAIConfigurationAppService : IApplicationService
{
    Task<AIScoringSettingsDto> GetScoringSettingsAsync();
    Task UpdateScoringSettingsAsync(UpdateAIScoringSettingsDto input);
}

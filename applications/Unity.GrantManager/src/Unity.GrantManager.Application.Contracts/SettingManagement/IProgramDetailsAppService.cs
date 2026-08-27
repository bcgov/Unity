using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.SettingManagement;

public interface IProgramDetailsAppService : IApplicationService
{
    Task<ProgramDetailsDto> GetProgramDetailsAsync();
    Task UpdateProgramDetailsAsync(UpdateProgramDetailsDto input);
}

using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface ISectionAppService : IApplicationService
    {        
        Task<ScoresheetSectionDto> GetAsync(Guid id);
        Task<ScoresheetSectionDto> UpdateAsync(EditSectionDto dto);
        Task DeleteAsync(Guid id);
    }
}

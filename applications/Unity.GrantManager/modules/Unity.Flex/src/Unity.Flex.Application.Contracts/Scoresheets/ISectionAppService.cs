using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface ISectionAppService : IApplicationService
    {        
        Task<ScoresheetSectionDto> GetAsync(Guid id);
        Task<ScoresheetSectionDto> UpdateAsync(Guid id, EditSectionDto dto);
        Task DeleteAsync(Guid id);
    }
}

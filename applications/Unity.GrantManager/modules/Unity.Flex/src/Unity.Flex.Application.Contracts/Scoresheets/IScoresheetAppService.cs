using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface IScoresheetAppService : IApplicationService
    {
        Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto);
        Task<QuestionDto> CreateQuestionAsync(Guid id, CreateQuestionDto dto);
        Task<ScoresheetSectionDto> CreateSectionAsync(Guid id, CreateSectionDto dto);
        Task DeleteAsync(Guid id);
        Task<ScoresheetDto> GetAsync(Guid id);
        Task<List<ScoresheetDto>> GetListAsync();
        Task SaveOrder(List<ScoresheetItemDto> dto);
        Task<ScoresheetDto> UpdateAsync(Guid id, EditScoresheetDto dto);
    }
}

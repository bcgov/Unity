using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface IScoresheetAppService : IApplicationService
    {
        Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto);
        Task<QuestionDto> CreateQuestionInHighestOrderSectionAsync(Guid scoresheetId, CreateQuestionDto dto);
        Task<ScoresheetSectionDto> CreateSectionAsync(Guid scoresheetId, CreateSectionDto dto);
        Task DeleteAsync(Guid id);
        Task<ClonedObjectDto> CloneScoresheetAsync(Guid scoresheetIdToClone, Guid? sectionIdToClone, Guid? questionIdToClone);
        Task<ScoresheetDto> GetAsync(Guid id);
        Task<List<ScoresheetDto>> GetListAsync(List<Guid> scoresheetIdsToLoad);
        Task<List<ScoresheetDto>> GetAllScoresheetsAsync();
        Task SaveOrder(List<ScoresheetItemDto> dto);
        Task UpdateAllAsync(Guid groupId, EditScoresheetsDto dto);
        Task<List<Guid>> GetNonDeletedQuestionIds(List<Guid> questionIdsToCheck);
    }
}

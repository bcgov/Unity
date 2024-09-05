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
        Task CloneScoresheetAsync(Guid scoresheetIdToClone);
        Task<ScoresheetDto> GetAsync(Guid id);
        Task<List<ScoresheetDto>> GetListAsync();
        Task<List<ScoresheetDto>> GetAllPublishedScoresheetsAsync();
        Task SaveOrder(List<ScoresheetItemDto> dto);
        Task UpdateAsync(Guid scoresheetId, EditScoresheetDto dto);
        Task<List<Guid>> GetNumericQuestionIdsAsync(List<Guid> questionIdsToCheck);
        Task<List<QuestionDto>> GetYesNoQuestionsAsync(List<Guid> questionIdsToCheck);
        Task<List<QuestionDto>> GetSelectListQuestionsAsync(List<Guid> questionIdsToCheck);
        Task ValidateChangeableScoresheet(Guid scoresheetId);
        Task PublishScoresheetAsync(Guid id);
        Task<ExportScoresheetDto> ExportScoresheet(Guid scoresheetId);
        Task ImportScoresheetAsync(ScoresheetImportDto scoresheetImportDto);
        Task SaveScoresheetOrder(List<Guid> scoresheetIds);
    }
}

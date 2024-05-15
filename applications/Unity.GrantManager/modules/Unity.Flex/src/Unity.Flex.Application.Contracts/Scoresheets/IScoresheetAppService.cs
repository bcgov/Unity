using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface IScoresheetAppService : IApplicationService
    {
        Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto);
        Task<QuestionDto> CreateQuestionAsync(CreateQuestionDto dto);
        Task<ScoresheetSectionDto> CreateSectionAsync(CreateSectionDto dto);
        Task<ScoresheetDto> EditAsync(EditScoresheetDto dto);
        Task<ScoresheetSectionDto> EditSectionAsync(EditSectionDto dto);
        Task<QuestionDto> EditQuestionAsync(EditQuestionDto dto);
        Task<ScoresheetDto> GetAsync(Guid scoresheetId);
        Task<QuestionDto> GetQuestionAsync(Guid questionId);
        Task<ScoresheetSectionDto> GetSectionAsync(Guid sectionId);
        Task<List<ScoresheetDto>> GetAllAsync();
    }
}

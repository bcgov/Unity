using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface IQuestionAppService : IApplicationService
    {        
        Task<QuestionDto> GetAsync(Guid id);
        Task<QuestionDto> UpdateAsync(Guid id, EditQuestionDto dto);
        Task DeleteAsync(Guid id);
    }
}

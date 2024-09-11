using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Scoresheets
{
    public interface IQuestionRepository : IBasicRepository<Question, Guid>
    {
        public Task<Question?> GetAsync(Guid questionId);
    }
}

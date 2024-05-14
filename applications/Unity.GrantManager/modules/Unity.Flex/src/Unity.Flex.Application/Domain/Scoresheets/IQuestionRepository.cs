using System;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Scoresheets
{
    public interface IQuestionRepository : IBasicRepository<Question, Guid>
    {
    }
}

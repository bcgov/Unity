using System;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class QuestionRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Question, Guid>(dbContextProvider), IQuestionRepository
    {
    }
}

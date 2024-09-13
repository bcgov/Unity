using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class QuestionRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, Question, Guid>(dbContextProvider), IQuestionRepository
    {
        public async Task<Question?> GetAsync(Guid questionId)
        {
            var dbContext = await GetDbContextAsync();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return await dbContext.Questions
                    .Include(q => q.Answers)
                    .Include(q => q.Section)
                    .ThenInclude(s => s.Scoresheet)
                    .FirstOrDefaultAsync(q => q.Id == questionId);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }
}

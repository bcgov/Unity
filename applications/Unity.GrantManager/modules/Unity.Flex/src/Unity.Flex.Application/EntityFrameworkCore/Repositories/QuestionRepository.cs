using System;
using System.Threading.Tasks;
using System.Linq;
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
            return await dbContext.Questions.Include(q => q.Answers)
                             .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public async Task<Question?> GetQuestionWithHighestOrderAsync(Guid sectionId)
        {
            var dbContext = await GetDbContextAsync();

            var highestOrderQuestion = await dbContext.Questions
                .Where(sec => sec.SectionId == sectionId)
                .OrderByDescending(sec => sec.Order)
                .FirstOrDefaultAsync();

            return highestOrderQuestion;
        }
    }
}

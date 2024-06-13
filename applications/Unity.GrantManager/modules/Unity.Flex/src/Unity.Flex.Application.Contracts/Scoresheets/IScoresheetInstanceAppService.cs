using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface IScoresheetInstanceAppService : IApplicationService
    {
        Task SaveAnswer(Guid assessmentId, Guid questionId, double answer);
    }
}

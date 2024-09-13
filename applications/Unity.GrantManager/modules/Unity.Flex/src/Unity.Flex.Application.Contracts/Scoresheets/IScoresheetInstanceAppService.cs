using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Scoresheets
{
    public interface IScoresheetInstanceAppService : IApplicationService
    {
        Task<ScoresheetInstanceDto?> CreateAsync(CreateScoresheetInstanceDto dto);
        Task<ScoresheetInstanceDto?> GetByCorrelationAsync(Guid id);
    }
}

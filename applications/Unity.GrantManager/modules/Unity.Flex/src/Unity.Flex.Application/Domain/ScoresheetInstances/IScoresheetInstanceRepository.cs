using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.ScoresheetInstances
{
    public interface IScoresheetInstanceRepository : IBasicRepository<ScoresheetInstance, Guid>
    {
        Task<ScoresheetInstance?> GetByCorrelationAsync(Guid correlationId);
    }
}

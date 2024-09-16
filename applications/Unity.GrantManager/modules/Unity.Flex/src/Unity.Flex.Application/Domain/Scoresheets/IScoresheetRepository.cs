using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Scoresheets
{
    public interface IScoresheetRepository : IBasicRepository<Scoresheet, Guid>
    {
        public Task<List<Scoresheet>> GetListWithChildrenAsync();
        public Task<List<Scoresheet>> GetPublishedListAsync();
        public Task<Scoresheet?> GetWithChildrenAsync(Guid id);
        Task<Scoresheet> GetAsync(Guid id, bool includeDetails = true);
        Task<Scoresheet?> GetByNameAsync(string name, bool includeDetails = false);
        Task<Scoresheet?> GetBySectionAsync(Guid id, bool includeDetails = false);
        Task<List<Scoresheet>> GetByNameStartsWithAsync(string name, bool includeDetails = false);
    }
}

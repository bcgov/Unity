using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.Flex.Domain.Scoresheets
{
    public interface IScoresheetRepository : IBasicRepository<Scoresheet, Guid>
    {
        public Task<List<Scoresheet>> GetListWithChildrenAsync();
    }
}

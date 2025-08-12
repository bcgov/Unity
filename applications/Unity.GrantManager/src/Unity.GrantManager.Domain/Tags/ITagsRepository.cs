using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GlobalTag;

public interface ITagsRepository : IRepository<Tag, Guid>
{
    Task<List<TagUsageSummary>> GetTagUsageSummaryAsync();
}

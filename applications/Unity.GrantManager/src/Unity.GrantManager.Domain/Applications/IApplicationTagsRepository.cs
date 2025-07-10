using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IApplicationTagsRepository : IRepository<ApplicationTags, Guid>
{
    Task<List<TagSummaryCount>> GetTagSummary();
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationTagsService : IApplicationService
{
    Task<IList<ApplicationTagsDto>> GetListAsync();
    Task<List<ApplicationTagsDto>> GetListWithApplicationIdsAsync(List<Guid> ids);
    Task<List<ApplicationTagsDto>> AssignTagsAsync(AssignApplicationTagsDto input);
    Task<List<ApplicationTagsDto>> GetApplicationTagsAsync(Guid id);
    Task<PagedResultDto<TagSummaryCountDto>> GetTagSummaryAsync();
    Task DeleteTagWithTagIdAsync(Guid id);
    Task DeleteTagAsync(Guid id);
}

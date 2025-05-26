using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationTagsService : IApplicationService
{
    Task<IList<ApplicationTagsDto>> GetListAsync();
    Task<IList<ApplicationTagsDto>> GetListWithApplicationIdsAsync(List<Guid> ids);

    Task<ApplicationTagsDto> CreateorUpdateTagsAsync(Guid id, ApplicationTagsDto input);

    Task<ApplicationTagsDto?> GetApplicationTagsAsync(Guid id);

    Task<PagedResultDto<TagSummaryCountDto>> GetTagSummaryAsync();
    Task<int> GetMaxRenameLengthAsync(string originalTag);
    Task<List<Guid>> RenameTagAsync(string originalTag, string replacementTag);
    Task RenameTagGlobalAsync(string originalTag, string replacementTag);
    Task DeleteTagAsync(string deleteTag);
    Task DeleteTagGlobalAsync(string deleteTag);
}

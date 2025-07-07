using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GlobalTag;

public interface ITagsService : IApplicationService
{
    Task<IList<TagDto>> GetListAsync();
   // Task<IList<ApplicationTagsDto>> GetListWithApplicationIdsAsync(List<Guid> ids);

    Task<TagDto> CreateorUpdateTagsAsync(Guid id, TagDto input);
    Task<TagDto> CreateTagsAsync (TagDto input);

   // Task<ApplicationTagsDto?> GetApplicationTagsAsync(Guid id);

    Task<PagedResultDto<TagUsageSummaryDto>> GetTagSummaryAsync();
    Task<int> GetMaxRenameLengthAsync(string originalTag);
    Task<List<Guid>> RenameTagAsync(Guid id, string originalTag, string replacementTag);
    Task RenameTagGlobalAsync(Guid id,string originalTag, string replacementTag);
    Task DeleteTagAsync(Guid id);
    Task DeleteTagGlobalAsync(Guid id);
}

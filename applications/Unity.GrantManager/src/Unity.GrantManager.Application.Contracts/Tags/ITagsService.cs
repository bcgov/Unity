using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GlobalTag;

public interface ITagsService : IApplicationService
{
    Task<IList<TagDto>> GetListAsync();
    Task<TagDto> CreateorUpdateTagsAsync(Guid id, TagDto input);
    Task<TagDto> CreateTagsAsync (TagDto input);
    Task<PagedResultDto<TagUsageSummaryDto>> GetTagSummaryAsync();
    Task<List<Guid>> RenameTagAsync(Guid id, string originalTag, string replacementTag);
    Task RenameTagGlobalAsync(Guid id,string originalTag, string replacementTag);
    Task DeleteTagAsync(Guid id);
    Task DeleteTagGlobalAsync(Guid id);
}

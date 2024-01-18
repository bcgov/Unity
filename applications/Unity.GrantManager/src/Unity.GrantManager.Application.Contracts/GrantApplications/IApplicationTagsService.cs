using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationTagsService : IApplicationService
{
    Task<IList<ApplicationTagsDto>> GetListAsync();
    Task<IList<ApplicationTagsDto>> GetListWithApplicationIdsAsync(List<Guid> ids);

    Task<ApplicationTagsDto> CreateorUpdateTagsAsync(Guid id, ApplicationTagsDto input);

    Task<ApplicationTagsDto?> GetApplicationTagsAsync(Guid id);

}

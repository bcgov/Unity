using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationTagsService : IApplicationService
{
    Task<IList<ApplicationTagsDto>> GetListAsync();

    Task<ApplicationTagsDto> CreateorUpdateTagsAsync(Guid id, ApplicationTagsDto input);


}

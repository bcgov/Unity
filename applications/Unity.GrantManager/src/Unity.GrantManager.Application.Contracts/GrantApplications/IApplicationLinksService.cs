using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationLinksService : ICrudAppService<
            ApplicationLinksDto,
            Guid>
{
    Task<List<ApplicationLinksInfoDto>> GetListByApplicationAsync(Guid applicationId);
    Task<ApplicationLinksInfoDto> GetLinkedApplicationAsync(Guid currentApplicationId, Guid linkedApplicationId);
    Task DeleteWithPairAsync(Guid applicationLinkId);

}

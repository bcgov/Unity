using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationLinksService : ICrudAppService<
            ApplicationLinksDto,
            Guid>
{
    Task<List<ApplicationLinksInfoDto>> GetListByApplicationAsync(Guid applicationId);
    Task<ApplicationLinksInfoDto> GetLinkedApplicationAsync(Guid currentApplicationId, Guid linkedApplicationId);
    Task<ApplicationLinksInfoDto> GetCurrentApplicationInfoAsync(Guid applicationId);
    Task DeleteWithPairAsync(Guid applicationLinkId);
    Task<ApplicationLinksInfoDto> GetApplicationDetailsByReferenceAsync(string referenceNumber);
    Task UpdateLinkTypeAsync(Guid applicationLinkId, ApplicationLinkType newLinkType);
    Task<ApplicationLinkValidationResult> ValidateApplicationLinksAsync(Guid currentApplicationId, List<ApplicationLinkValidationRequest> proposedLinks);
}

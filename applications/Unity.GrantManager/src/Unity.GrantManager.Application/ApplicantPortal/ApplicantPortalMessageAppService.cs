using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicantPortal;

[Authorize]
public class ApplicantPortalMessageAppService(ApplicantPortalMessageService messageService)
    : ApplicationService, IApplicantPortalMessageAppService
{
    public virtual Task<MultipleIdentitiesMessageDto> GetMultipleIdentitiesAsync() => messageService.GetAsync();

    public virtual Task<MultipleIdentitiesMessageDto> UpdateMultipleIdentitiesAsync(UpdateMultipleIdentitiesMessageDto input) =>
        messageService.UpdateAsync(input);
}

using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicantPortal;

public interface IApplicantPortalMessageAppService : IApplicationService
{
    Task<MultipleIdentitiesMessageDto> GetMultipleIdentitiesAsync();
    Task<MultipleIdentitiesMessageDto> UpdateMultipleIdentitiesAsync(UpdateMultipleIdentitiesMessageDto input);
}

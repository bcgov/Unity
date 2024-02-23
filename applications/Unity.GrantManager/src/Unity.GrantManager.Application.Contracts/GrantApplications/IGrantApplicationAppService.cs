using System;
using System.Threading.Tasks;
using Unity.GrantManager.Comments;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IGrantApplicationAppService : ICrudAppService<
            GrantApplicationDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateGrantApplicationDto>, ICommentsService
    {
        Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid id);
        Task<ListResultDto<ApplicationActionDto>> GetActions(Guid applicationId, bool includeInternal = false);
        Task<GetSummaryDto> GetSummaryAsync(Guid applicationId);

        Task<GrantApplicationDto> UpdateProjectInfoAsync(Guid id, CreateUpdateProjectInfoDto input);
        Task<GrantApplicationDto> UpdateAssessmentResultsAsync(Guid id, CreateUpdateAssessmentResultsDto input);
    }
}

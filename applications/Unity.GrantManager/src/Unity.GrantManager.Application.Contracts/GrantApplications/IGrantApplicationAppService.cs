using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Comments;
using Unity.Modules.Shared;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public interface IGrantApplicationAppService : ICommentsService
    {
        Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid id);
        Task<ListResultDto<ApplicationActionDto>> GetActions(Guid applicationId, bool includeInternal = false);
        Task<GetSummaryDto> GetSummaryAsync(Guid applicationId);
        Task<GrantApplicationDto> UpdateProjectInfoAsync(Guid id, CreateUpdateProjectInfoDto input);
        Task<GrantApplicationDto> UpdatePartialProjectInfoAsync(Guid id, PartialUpdateDto<UpdateProjectInfoDto> input);
        Task<GrantApplicationDto> UpdateProjectApplicantInfoAsync(Guid id, CreateUpdateApplicantInfoDto input);
        Task<GrantApplicationDto> UpdatePartialApplicantInfoAsync(Guid id, PartialUpdateDto<UpsertApplicantInfoDto> input);
        Task<GrantApplicationDto> UpdateAssessmentResultsAsync(Guid id, CreateUpdateAssessmentResultsDto input);
        Task UpdateSupplierNumberAsync(Guid applicationId, string supplierNumber);
        Task<List<GrantApplicationLiteDto>> GetAllApplicationsAsync();
        Task<IList<GrantApplicationDto>> GetApplicationDetailsListAsync(List<Guid> applicationIds);
        Task<GrantApplicationDto> GetAsync(Guid id);
        Task<PagedResultDto<GrantApplicationDto>> GetListAsync(PagedAndSortedResultRequestDto input);
        Task<GrantApplicationDto> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction);
    }
}

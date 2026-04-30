using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Modules.Shared;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public interface IGrantApplicationAppService
{
    Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid id);
    Task<ListResultDto<ApplicationActionDto>> GetActions(Guid applicationId, bool includeInternal = false);        
    Task<GrantApplicationDto> UpdateProjectInfoAsync(Guid id, CreateUpdateProjectInfoDto input);
    Task<GrantApplicationDto> UpdatePartialProjectInfoAsync(Guid id, PartialUpdateDto<UpdateProjectInfoDto> input);        
    Task<GrantApplicationDto> UpdateAssessmentResultsAsync(Guid id, CreateUpdateAssessmentResultsDto input);
    Task UpdateSupplierNumberAsync(Guid applicationId, string supplierNumber);
    Task<List<GrantApplicationLiteDto>> GetAllApplicationsAsync();
    Task<IList<GrantApplicationDto>> GetApplicationDetailsListAsync(List<Guid> applicationIds);
    Task<GrantApplicationDto> GetAsync(Guid id);
    Task<GrantApplicationDto> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction);
    Task<AIGenerationRequestDto> QueueAIGenerationAsync(Guid applicationId, string? promptVersion = null);
    Task<AIGenerationRequestDto> QueueApplicationAnalysisAsync(Guid applicationId, string? promptVersion = null);
    Task<AIGenerationRequestDto> QueueAttachmentSummaryAsync(Guid applicationId, string? promptVersion = null);
    Task<AIGenerationRequestDto> QueueApplicationScoringAsync(Guid applicationId, string? promptVersion = null);
    Task<AIGenerationRequestDto?> GetAIGenerationStatusAsync(Guid applicationId, string operationType, string? promptVersion = null);
    Task<AIGenerationRequestDto> QueueAllAIStagesAsync(Guid applicationId, string? promptVersion = null);
    Task<Guid?> GetAccountCodingIdFromFormIdAsync(Guid formId);
    Task<string> DismissAIAnalysisItemAsync(Guid applicationId, string itemId);
    Task<string> RestoreAIAnalysisItemAsync(Guid applicationId, string itemId);
    Task<PagedResultDto<GrantApplicationDto>> GetListAsync(GrantApplicationListInputDto input);
    Task<bool> IsApplicantRedStopAsync(Guid applicationId);
}

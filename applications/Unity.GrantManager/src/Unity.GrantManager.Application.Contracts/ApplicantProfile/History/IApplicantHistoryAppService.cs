using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicantProfile;

public interface IApplicantHistoryAppService : IApplicationService
{
    Task<List<FundingHistoryDto>> GetFundingHistoryListAsync(Guid applicantId);
    Task<FundingHistoryDto> GetFundingHistoryAsync(Guid id);
    Task<FundingHistoryDto> CreateFundingHistoryAsync(CreateUpdateFundingHistoryDto input);
    Task<FundingHistoryDto> UpdateFundingHistoryAsync(Guid id, CreateUpdateFundingHistoryDto input);
    Task DeleteFundingHistoryAsync(Guid id);

    Task<List<IssueTrackingDto>> GetIssueTrackingListAsync(Guid applicantId);
    Task<IssueTrackingDto> GetIssueTrackingAsync(Guid id);
    Task<IssueTrackingDto> CreateIssueTrackingAsync(CreateUpdateIssueTrackingDto input);
    Task<IssueTrackingDto> UpdateIssueTrackingAsync(Guid id, CreateUpdateIssueTrackingDto input);
    Task DeleteIssueTrackingAsync(Guid id);

    Task<List<AuditHistoryDto>> GetAuditHistoryListAsync(Guid applicantId);
    Task<AuditHistoryDto> GetAuditHistoryAsync(Guid id);
    Task<AuditHistoryDto> CreateAuditHistoryAsync(CreateUpdateAuditHistoryDto input);
    Task<AuditHistoryDto> UpdateAuditHistoryAsync(Guid id, CreateUpdateAuditHistoryDto input);
    Task DeleteAuditHistoryAsync(Guid id);

    Task SaveNotesAsync(Guid applicantId, SaveApplicantHistoryNotesDto input);
}

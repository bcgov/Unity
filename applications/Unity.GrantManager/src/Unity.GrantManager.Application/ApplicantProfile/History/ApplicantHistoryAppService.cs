using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.ApplicantProfile;

public class ApplicantHistoryAppService(
    IFundingHistoryRepository fundingHistoryRepository,
    IIssueTrackingRepository issueTrackingRepository,
    IAuditHistoryRepository auditHistoryRepository,
    IApplicantRepository applicantRepository) : GrantManagerAppService, IApplicantHistoryAppService
{
    public async Task<List<FundingHistoryDto>> GetFundingHistoryListAsync(Guid applicantId)
    {
        var items = await fundingHistoryRepository.GetByApplicantIdAsync(applicantId);
        return ObjectMapper.Map<List<FundingHistory>, List<FundingHistoryDto>>(items);
    }

    public async Task<FundingHistoryDto> GetFundingHistoryAsync(Guid id)
    {
        var entity = await fundingHistoryRepository.GetAsync(id);
        return ObjectMapper.Map<FundingHistory, FundingHistoryDto>(entity);
    }

    public async Task<FundingHistoryDto> CreateFundingHistoryAsync(CreateUpdateFundingHistoryDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateFundingHistoryDto, FundingHistory>(input);
        await fundingHistoryRepository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<FundingHistory, FundingHistoryDto>(entity);
    }

    public async Task<FundingHistoryDto> UpdateFundingHistoryAsync(Guid id, CreateUpdateFundingHistoryDto input)
    {
        var entity = await fundingHistoryRepository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await fundingHistoryRepository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<FundingHistory, FundingHistoryDto>(entity);
    }

    public async Task DeleteFundingHistoryAsync(Guid id)
    {
        await fundingHistoryRepository.DeleteAsync(id, autoSave: true);
    }

    public async Task<List<IssueTrackingDto>> GetIssueTrackingListAsync(Guid applicantId)
    {
        var items = await issueTrackingRepository.GetByApplicantIdAsync(applicantId);
        return ObjectMapper.Map<List<IssueTracking>, List<IssueTrackingDto>>(items);
    }

    public async Task<IssueTrackingDto> GetIssueTrackingAsync(Guid id)
    {
        var entity = await issueTrackingRepository.GetAsync(id);
        return ObjectMapper.Map<IssueTracking, IssueTrackingDto>(entity);
    }

    public async Task<IssueTrackingDto> CreateIssueTrackingAsync(CreateUpdateIssueTrackingDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateIssueTrackingDto, IssueTracking>(input);
        await issueTrackingRepository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<IssueTracking, IssueTrackingDto>(entity);
    }

    public async Task<IssueTrackingDto> UpdateIssueTrackingAsync(Guid id, CreateUpdateIssueTrackingDto input)
    {
        var entity = await issueTrackingRepository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await issueTrackingRepository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<IssueTracking, IssueTrackingDto>(entity);
    }

    public async Task DeleteIssueTrackingAsync(Guid id)
    {
        await issueTrackingRepository.DeleteAsync(id, autoSave: true);
    }

    public async Task<List<AuditHistoryDto>> GetAuditHistoryListAsync(Guid applicantId)
    {
        var items = await auditHistoryRepository.GetByApplicantIdAsync(applicantId);
        return ObjectMapper.Map<List<AuditHistory>, List<AuditHistoryDto>>(items);
    }

    public async Task<AuditHistoryDto> GetAuditHistoryAsync(Guid id)
    {
        var entity = await auditHistoryRepository.GetAsync(id);
        return ObjectMapper.Map<AuditHistory, AuditHistoryDto>(entity);
    }

    public async Task<AuditHistoryDto> CreateAuditHistoryAsync(CreateUpdateAuditHistoryDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateAuditHistoryDto, AuditHistory>(input);
        await auditHistoryRepository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<AuditHistory, AuditHistoryDto>(entity);
    }

    public async Task<AuditHistoryDto> UpdateAuditHistoryAsync(Guid id, CreateUpdateAuditHistoryDto input)
    {
        var entity = await auditHistoryRepository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await auditHistoryRepository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<AuditHistory, AuditHistoryDto>(entity);
    }

    public async Task DeleteAuditHistoryAsync(Guid id)
    {
        await auditHistoryRepository.DeleteAsync(id, autoSave: true);
    }

    public async Task SaveNotesAsync(Guid applicantId, SaveApplicantHistoryNotesDto input)
    {
        var applicant = await applicantRepository.GetAsync(applicantId);
        applicant.FundingHistoryComments = input.FundingHistoryComments;
        applicant.IssueTrackingComments = input.IssueTrackingComments;
        applicant.AuditComments = input.AuditComments;
        await applicantRepository.UpdateAsync(applicant, autoSave: true);
    }
}

using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;

namespace Unity.GrantManager.ApplicantProfile;

[Authorize(UnitySelector.ApplicantManagement.History.Default)]
public class ApplicantHistoryAppService(
    IFundingHistoryRepository fundingHistoryRepository,
    IIssueTrackingRepository issueTrackingRepository,
    IAuditHistoryRepository auditHistoryRepository,
    IReportsHistoryRepository reportsHistoryRepository,
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

    [Authorize(UnitySelector.ApplicantManagement.History.FundingHistory.Update)]
    public async Task<FundingHistoryDto> CreateFundingHistoryAsync(CreateUpdateFundingHistoryDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateFundingHistoryDto, FundingHistory>(input);
        await fundingHistoryRepository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<FundingHistory, FundingHistoryDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.FundingHistory.Update)]
    public async Task<FundingHistoryDto> UpdateFundingHistoryAsync(Guid id, CreateUpdateFundingHistoryDto input)
    {
        var entity = await fundingHistoryRepository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await fundingHistoryRepository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<FundingHistory, FundingHistoryDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.FundingHistory.Update)]
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

    [Authorize(UnitySelector.ApplicantManagement.History.IssueHistory.Update)]
    public async Task<IssueTrackingDto> CreateIssueTrackingAsync(CreateUpdateIssueTrackingDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateIssueTrackingDto, IssueTracking>(input);
        await issueTrackingRepository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<IssueTracking, IssueTrackingDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.IssueHistory.Update)]
    public async Task<IssueTrackingDto> UpdateIssueTrackingAsync(Guid id, CreateUpdateIssueTrackingDto input)
    {
        var entity = await issueTrackingRepository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await issueTrackingRepository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<IssueTracking, IssueTrackingDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.IssueHistory.Update)]
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

    [Authorize(UnitySelector.ApplicantManagement.History.AuditHistory.Update)]
    public async Task<AuditHistoryDto> CreateAuditHistoryAsync(CreateUpdateAuditHistoryDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateAuditHistoryDto, AuditHistory>(input);
        await auditHistoryRepository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<AuditHistory, AuditHistoryDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.AuditHistory.Update)]
    public async Task<AuditHistoryDto> UpdateAuditHistoryAsync(Guid id, CreateUpdateAuditHistoryDto input)
    {
        var entity = await auditHistoryRepository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await auditHistoryRepository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<AuditHistory, AuditHistoryDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.AuditHistory.Update)]
    public async Task DeleteAuditHistoryAsync(Guid id)
    {
        await auditHistoryRepository.DeleteAsync(id, autoSave: true);
    }

    public async Task<List<ReportsHistoryDto>> GetReportsHistoryListAsync(Guid applicantId)
    {
        var items = await reportsHistoryRepository.GetByApplicantIdAsync(applicantId);
        return ObjectMapper.Map<List<ReportsHistory>, List<ReportsHistoryDto>>(items);
    }

    public async Task<ReportsHistoryDto> GetReportsHistoryAsync(Guid id)
    {
        var entity = await reportsHistoryRepository.GetAsync(id);
        return ObjectMapper.Map<ReportsHistory, ReportsHistoryDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.ReportsHistory.Update)]
    public async Task<ReportsHistoryDto> CreateReportsHistoryAsync(CreateUpdateReportsHistoryDto input)
    {
        var entity = ObjectMapper.Map<CreateUpdateReportsHistoryDto, ReportsHistory>(input);
        await reportsHistoryRepository.InsertAsync(entity, autoSave: true);
        return ObjectMapper.Map<ReportsHistory, ReportsHistoryDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.ReportsHistory.Update)]
    public async Task<ReportsHistoryDto> UpdateReportsHistoryAsync(Guid id, CreateUpdateReportsHistoryDto input)
    {
        var entity = await reportsHistoryRepository.GetAsync(id);
        ObjectMapper.Map(input, entity);
        await reportsHistoryRepository.UpdateAsync(entity, autoSave: true);
        return ObjectMapper.Map<ReportsHistory, ReportsHistoryDto>(entity);
    }

    [Authorize(UnitySelector.ApplicantManagement.History.ReportsHistory.Update)]
    public async Task DeleteReportsHistoryAsync(Guid id)
    {
        await reportsHistoryRepository.DeleteAsync(id, autoSave: true);
    }

    public async Task SaveNotesAsync(Guid applicantId, SaveApplicantHistoryNotesDto input)
    {
        // Check if the user has permission to update any of the applicant history notes
        if (!await AuthorizationService.IsGrantedAnyAsync(
            UnitySelector.ApplicantManagement.History.FundingHistory.Update,
            UnitySelector.ApplicantManagement.History.AuditHistory.Update,
            UnitySelector.ApplicantManagement.History.IssueHistory.Update,
            UnitySelector.ApplicantManagement.History.ReportsHistory.Update
        ))
        {
            throw new UnauthorizedAccessException("You do not have permission to update any applicant history notes.");
        }

        var modifiedFields = input.ModifiedFields.Count > 0
            ? new HashSet<string>(input.ModifiedFields, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>([
                nameof(SaveApplicantHistoryNotesDto.FundingHistoryComments),
                nameof(SaveApplicantHistoryNotesDto.IssueTrackingComments),
                nameof(SaveApplicantHistoryNotesDto.AuditComments),
                nameof(SaveApplicantHistoryNotesDto.ReportsComments)
            ], StringComparer.OrdinalIgnoreCase);

        var applicant = await applicantRepository.GetAsync(applicantId);

        if (modifiedFields.Contains(nameof(SaveApplicantHistoryNotesDto.FundingHistoryComments))
            && await AuthorizationService.IsGrantedAsync(UnitySelector.ApplicantManagement.History.FundingHistory.Update))
        {
            applicant.FundingHistoryComments = input.FundingHistoryComments;
        }

        if (modifiedFields.Contains(nameof(SaveApplicantHistoryNotesDto.IssueTrackingComments))
            && await AuthorizationService.IsGrantedAsync(UnitySelector.ApplicantManagement.History.IssueHistory.Update))
        {
            applicant.IssueTrackingComments = input.IssueTrackingComments;
        }

        if (modifiedFields.Contains(nameof(SaveApplicantHistoryNotesDto.AuditComments))
            && await AuthorizationService.IsGrantedAsync(UnitySelector.ApplicantManagement.History.AuditHistory.Update))
        {
            applicant.AuditComments = input.AuditComments;
        }

        if (modifiedFields.Contains(nameof(SaveApplicantHistoryNotesDto.ReportsComments))
            && await AuthorizationService.IsGrantedAsync(UnitySelector.ApplicantManagement.History.ReportsHistory.Update))
        {
            applicant.ReportsComments = input.ReportsComments;
        }

        await applicantRepository.UpdateAsync(applicant, autoSave: true);
    }
}

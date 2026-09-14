using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Payments;
using Unity.Modules.Shared;
using Unity.Payments.PaymentRequests;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applicants;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantMergeAppService), typeof(IApplicantMergeAppService))]
public class ApplicantMergeAppService(
    ApplicantMergeManager mergeManager,
    IApplicantRepository applicantRepository,
    IApplicationRepository applicationRepository,
    IPaymentRequestAppService paymentRequestService) : GrantManagerAppService, IApplicantMergeAppService
{
    [HttpPost("api/app/applicant-merge")]
    public virtual async Task<ApplicantMergeDto> MergeAsync(MergeApplicantsDto input)
    {
        await AuthorizeMergeAsync();
        ValidateApplicantIds(input.PrincipalApplicantId, input.SecondaryApplicantId);
        await EnsureNoPendingPaymentsAsync(input.PrincipalApplicantId, input.SecondaryApplicantId);

        var operation = await mergeManager.MergeAsync(
            input.PrincipalApplicantId,
            input.SecondaryApplicantId,
            ToDomainValues(input.Summary),
            input.SelectedSupplierId,
            input.Source,
            CurrentTenant.Id,
            CurrentUser.Id);

        return ToDto(operation);
    }

    [Authorize(UnitySelector.ApplicantManagement.Applicant.Unmerge)]
    [HttpGet("api/app/applicant-merge/reversible")]
    public virtual async Task<ApplicantMergeListDto> GetReversibleMergesAsync(Guid applicantId)
    {
        if (applicantId == Guid.Empty)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeApplicantUnavailable);
        }

        var operations = await mergeManager.GetActiveOperationsAsync(applicantId);
        var previews = new List<ApplicantMergePreviewDto>();
        foreach (var operation in operations)
        {
            previews.Add(await CreatePreviewAsync(operation.Id));
        }

        return new ApplicantMergeListDto { Items = previews };
    }

    [Authorize(UnitySelector.ApplicantManagement.Applicant.Unmerge)]
    [HttpGet("api/app/applicant-merge/{mergeOperationId}/preview")]
    public virtual async Task<ApplicantMergePreviewDto> GetUnmergePreviewAsync(Guid mergeOperationId)
    {
        return await CreatePreviewAsync(mergeOperationId);
    }

    [Authorize(UnitySelector.ApplicantManagement.Applicant.Unmerge)]
    [HttpPost("api/app/applicant-merge/{mergeOperationId}/unmerge")]
    public virtual async Task<ApplicantMergeDto> UnmergeAsync(
        Guid mergeOperationId,
        UnmergeApplicantsDto input)
    {
        var reversibility = await mergeManager.GetReversibilityAsync(mergeOperationId);
        if (!reversibility.CanReverse)
        {
            throw new BusinessException(
                reversibility.ErrorCode ?? GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
        }

        await EnsureNoPendingPaymentsAsync(
            reversibility.Operation.PrincipalApplicantId,
            reversibility.Operation.SecondaryApplicantId);

        var operation = await mergeManager.UnmergeAsync(
            mergeOperationId,
            CurrentUser.Id,
            input.Reason);

        return ToDto(operation);
    }

    protected virtual async Task<ApplicantMergePreviewDto> CreatePreviewAsync(Guid mergeOperationId)
    {
        var reversibility = await mergeManager.GetReversibilityAsync(mergeOperationId);
        var operation = reversibility.Operation;
        var principal = await applicantRepository.FindAsync(operation.PrincipalApplicantId);
        var secondary = await applicantRepository.FindAsync(operation.SecondaryApplicantId);
        var hasPendingPayments = await HasPendingPaymentsAsync(
            operation.PrincipalApplicantId,
            operation.SecondaryApplicantId);
        var errorCode = hasPendingPayments
            ? GrantManagerDomainErrorCodes.ApplicantMergePendingPayments
            : reversibility.ErrorCode;

        var dto = new ApplicantMergePreviewDto
        {
            PrincipalApplicantName = principal?.ApplicantName ?? operation.PrincipalApplicantId.ToString(),
            SecondaryApplicantName = secondary?.ApplicantName ?? operation.SecondaryApplicantId.ToString(),
            CanUnmerge = reversibility.CanReverse && !hasPendingPayments,
            BlockReason = errorCode == null ? null : L[errorCode].Value
        };
        CopyOperation(operation, dto);
        return dto;
    }

    protected virtual async Task AuthorizeMergeAsync()
    {
        var isGranted = await AuthorizationService.IsGrantedAnyAsync(
            UnitySelector.ApplicantManagement.Applicant.Merge,
            UnitySelector.Applicant.Summary.Update_AssignApplicant);

        if (!isGranted)
        {
            throw new AbpAuthorizationException("You do not have permission to merge applicants.");
        }
    }

    protected virtual async Task EnsureNoPendingPaymentsAsync(Guid principalId, Guid secondaryId)
    {
        if (await HasPendingPaymentsAsync(principalId, secondaryId))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergePendingPayments);
        }
    }

    protected virtual async Task<bool> HasPendingPaymentsAsync(Guid principalId, Guid secondaryId)
    {
        if (!await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature))
        {
            return false;
        }

        var principalApplicationIds = (await applicationRepository.GetByApplicantIdAsync(principalId))
            .Select(item => item.Id);
        var secondaryApplicationIds = (await applicationRepository.GetByApplicantIdAsync(secondaryId))
            .Select(item => item.Id);
        var applicationIds = principalApplicationIds.Concat(secondaryApplicationIds).Distinct().ToList();
        if (applicationIds.Count == 0)
        {
            return false;
        }

        var pendingPayments = await paymentRequestService
            .GetPaymentPendingListByCorrelationIdsAsync(applicationIds);
        return pendingPayments is { Count: > 0 };
    }

    private static ApplicantMergeValues ToDomainValues(ApplicantMergeSummaryDto summary)
    {
        return new ApplicantMergeValues
        {
            ApplicantName = summary.ApplicantName,
            UnityApplicantId = summary.UnityApplicantId,
            OrgName = summary.OrgName,
            OrgNumber = summary.OrgNumber,
            NonRegOrgName = summary.NonRegOrgName,
            OrganizationType = summary.OrganizationType,
            ApproxNumberOfEmployees = summary.ApproxNumberOfEmployees,
            OrgStatus = summary.OrgStatus,
            IndigenousOrgInd = summary.IndigenousOrgInd,
            Sector = summary.Sector,
            SubSector = summary.SubSector,
            SectorSubSectorIndustryDesc = summary.SectorSubSectorIndustryDesc,
            FiscalDay = summary.FiscalDay,
            FiscalMonth = summary.FiscalMonth
        };
    }

    private static ApplicantMergeDto ToDto(ApplicantMergeOperation operation)
    {
        var dto = new ApplicantMergeDto();
        CopyOperation(operation, dto);
        return dto;
    }

    private static void CopyOperation(ApplicantMergeOperation operation, ApplicantMergeDto dto)
    {
        dto.Id = operation.Id;
        dto.PrincipalApplicantId = operation.PrincipalApplicantId;
        dto.SecondaryApplicantId = operation.SecondaryApplicantId;
        dto.Status = operation.Status;
        dto.Source = operation.Source;
        dto.MergedAt = operation.MergedAt;
        dto.MergedById = operation.MergedById;
        dto.ReversedAt = operation.ReversedAt;
        dto.ReversedById = operation.ReversedById;
        dto.ReversalReason = operation.ReversalReason;
        dto.TransferredApplicationCount = operation.ApplicationChanges.Count(item => item.WasTransferred);
    }

    private static void ValidateApplicantIds(Guid principalId, Guid secondaryId)
    {
        if (principalId == Guid.Empty || secondaryId == Guid.Empty)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeApplicantUnavailable);
        }

        if (principalId == secondaryId)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeSameApplicant);
        }
    }
}

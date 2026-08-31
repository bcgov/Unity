using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications;

public class ApplicantMergeManager(
    IApplicantRepository applicantRepository,
    IApplicationRepository applicationRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicantAgentRepository applicantAgentRepository,
    IApplicantAddressRepository applicantAddressRepository,
    IApplicantMergeOperationRepository mergeOperationRepository) : DomainService
{
    private const int SnapshotVersion = 1;
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public virtual async Task<ApplicantMergeOperation> MergeAsync(
        Guid principalApplicantId,
        Guid secondaryApplicantId,
        ApplicantMergeValues values,
        Guid? selectedSupplierId,
        ApplicantMergeSource source,
        Guid? tenantId,
        Guid? mergedById)
    {
        if (principalApplicantId == secondaryApplicantId)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeSameApplicant);
        }

        var principal = await applicantRepository.FindAsync(principalApplicantId);
        var secondary = await applicantRepository.FindAsync(secondaryApplicantId);

        EnsureApplicantsAvailable(principal, secondary);

        if (!values.IsComposedFrom(principal!, secondary!))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeInvalidSelection);
        }

        if (selectedSupplierId != principal!.SupplierId && selectedSupplierId != secondary!.SupplierId)
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeInvalidSupplier);
        }

        var principalBefore = ApplicantMergeApplicantSnapshot.FromApplicant(principal);
        var secondaryBefore = ApplicantMergeApplicantSnapshot.FromApplicant(secondary);
        // Merge updates applications, so load only the tracked application entities.
        // GetByApplicantIdAsync is a read-oriented no-tracking query that includes
        // the Applicant graph; attaching that graph for update conflicts with the
        // principal and secondary Applicant instances already tracked above.
        var principalApplications = await applicationRepository.GetListAsync(
            application => application.ApplicantId == principalApplicantId);
        var secondaryApplications = await applicationRepository.GetListAsync(
            application => application.ApplicantId == secondaryApplicantId);
        var mergeOperationId = GuidGenerator.Create();
        var applicationChanges = new List<ApplicantMergeApplicationChange>();

        foreach (var application in principalApplications)
        {
            applicationChanges.Add(CreateApplicationChange(
                mergeOperationId,
                tenantId,
                application,
                wasTransferred: false,
                applicantIdBefore: principalApplicantId,
                applicantIdAfter: principalApplicantId,
                new ApplicantMergeRelatedRecordsSnapshot()));

            application.DefaultSiteId = null;
            await applicationRepository.UpdateAsync(application);
        }

        foreach (var application in secondaryApplications)
        {
            var formSubmissions = await applicationFormSubmissionRepository.GetListAsync(
                item => item.ApplicationId == application.Id && item.ApplicantId == secondaryApplicantId);
            var applicantAgents = await applicantAgentRepository.GetListAsync(
                item => item.ApplicationId == application.Id && item.ApplicantId == secondaryApplicantId);
            var applicantAddresses = await applicantAddressRepository
                .FindByApplicantIdAndApplicationIdAsync(secondaryApplicantId, application.Id);

            var relatedRecords = new ApplicantMergeRelatedRecordsSnapshot
            {
                ApplicationFormSubmissionIds = formSubmissions.Select(item => item.Id).ToList(),
                ApplicantAgentIds = applicantAgents.Select(item => item.Id).ToList(),
                ApplicantAddressIds = applicantAddresses.Select(item => item.Id).ToList()
            };

            applicationChanges.Add(CreateApplicationChange(
                mergeOperationId,
                tenantId,
                application,
                wasTransferred: true,
                applicantIdBefore: secondaryApplicantId,
                applicantIdAfter: principalApplicantId,
                relatedRecords));

            application.ApplicantId = principalApplicantId;
            application.DefaultSiteId = null;
            await applicationRepository.UpdateAsync(application);

            foreach (var submission in formSubmissions)
            {
                submission.ApplicantId = principalApplicantId;
                await applicationFormSubmissionRepository.UpdateAsync(submission);
            }

            foreach (var agent in applicantAgents)
            {
                agent.ApplicantId = principalApplicantId;
                await applicantAgentRepository.UpdateAsync(agent);
            }

            foreach (var address in applicantAddresses)
            {
                address.ApplicantId = principalApplicantId;
                await applicantAddressRepository.UpdateAsync(address);
            }
        }

        values.ApplyTo(principal);
        principal.SupplierId = selectedSupplierId;
        principal.IsDuplicated = false;
        secondary.SupplierId = selectedSupplierId;
        secondary.IsDuplicated = true;

        await applicantRepository.UpdateAsync(principal);
        await applicantRepository.UpdateAsync(secondary);

        var operation = new ApplicantMergeOperation(
            mergeOperationId,
            tenantId,
            principalApplicantId,
            secondaryApplicantId,
            source,
            Serialize(principalBefore),
            Serialize(ApplicantMergeApplicantSnapshot.FromApplicant(principal)),
            Serialize(secondaryBefore),
            Serialize(ApplicantMergeApplicantSnapshot.FromApplicant(secondary)),
            Clock.Now,
            mergedById,
            SnapshotVersion);

        foreach (var applicationChange in applicationChanges)
        {
            operation.AddApplicationChange(applicationChange);
        }

        return await mergeOperationRepository.InsertAsync(operation, autoSave: true);
    }

    public virtual async Task<List<ApplicantMergeOperation>> GetActiveOperationsAsync(Guid applicantId)
    {
        return await mergeOperationRepository.GetActiveForApplicantAsync(applicantId);
    }

    public virtual async Task<ApplicantMergeReversibility> GetReversibilityAsync(Guid mergeOperationId)
    {
        var operation = await mergeOperationRepository.GetWithChangesAsync(mergeOperationId);

        if (operation.Status != ApplicantMergeStatus.Completed)
        {
            return new ApplicantMergeReversibility(
                operation,
                false,
                GrantManagerDomainErrorCodes.ApplicantMergeAlreadyReversed);
        }

        if (operation.SnapshotVersion != SnapshotVersion)
        {
            return new ApplicantMergeReversibility(
                operation,
                false,
                GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
        }

        if (await mergeOperationRepository.HasLaterActiveMergeAsync(operation))
        {
            return new ApplicantMergeReversibility(
                operation,
                false,
                GrantManagerDomainErrorCodes.ApplicantMergeNotLatest);
        }

        var principal = await applicantRepository.FindAsync(operation.PrincipalApplicantId);
        var secondary = await applicantRepository.FindAsync(operation.SecondaryApplicantId);

        if (!ApplicantsAvailable(principal, secondary))
        {
            return new ApplicantMergeReversibility(
                operation,
                false,
                GrantManagerDomainErrorCodes.ApplicantMergeApplicantUnavailable);
        }

        try
        {
            var principalAfter = Deserialize<ApplicantMergeApplicantSnapshot>(operation.PrincipalStateAfter);
            var secondaryAfter = Deserialize<ApplicantMergeApplicantSnapshot>(operation.SecondaryStateAfter);
            if (ApplicantMergeApplicantSnapshot.FromApplicant(principal!) != principalAfter
                || ApplicantMergeApplicantSnapshot.FromApplicant(secondary!) != secondaryAfter)
            {
                return new ApplicantMergeReversibility(
                    operation,
                    false,
                    GrantManagerDomainErrorCodes.ApplicantMergeStateChanged);
            }

            if (!await RelatedRecordsMatchAfterStateAsync(operation))
            {
                return new ApplicantMergeReversibility(
                    operation,
                    false,
                    GrantManagerDomainErrorCodes.ApplicantMergeRelatedRecordsChanged);
            }
        }
        catch (JsonException)
        {
            return new ApplicantMergeReversibility(
                operation,
                false,
                GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
        }
        catch (BusinessException exception)
            when (exception.Code == GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory)
        {
            return new ApplicantMergeReversibility(
                operation,
                false,
                GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
        }

        return new ApplicantMergeReversibility(operation, true, null);
    }

    public virtual async Task<ApplicantMergeOperation> UnmergeAsync(
        Guid mergeOperationId,
        Guid? reversedById,
        string reason)
    {
        var reversibility = await GetReversibilityAsync(mergeOperationId);
        if (!reversibility.CanReverse)
        {
            throw new BusinessException(
                reversibility.ErrorCode ?? GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
        }

        var operation = reversibility.Operation;
        var principal = await applicantRepository.GetAsync(operation.PrincipalApplicantId);
        var secondary = await applicantRepository.GetAsync(operation.SecondaryApplicantId);

        foreach (var change in operation.ApplicationChanges)
        {
            var application = await applicationRepository.GetAsync(change.ApplicationId);
            application.ApplicantId = change.ApplicantIdBefore;
            application.DefaultSiteId = change.DefaultSiteIdBefore;
            await applicationRepository.UpdateAsync(application);

            if (!change.WasTransferred)
            {
                continue;
            }

            var relatedRecords = Deserialize<ApplicantMergeRelatedRecordsSnapshot>(change.RelatedRecordsSnapshot);
            var formSubmissions = await applicationFormSubmissionRepository.GetListAsync(
                item => relatedRecords.ApplicationFormSubmissionIds.Contains(item.Id));
            var applicantAgents = await applicantAgentRepository.GetListAsync(
                item => relatedRecords.ApplicantAgentIds.Contains(item.Id));
            var applicantAddresses = await applicantAddressRepository
                .FindByApplicantIdAndApplicationIdAsync(change.ApplicantIdAfter, change.ApplicationId);

            foreach (var submission in formSubmissions)
            {
                submission.ApplicantId = change.ApplicantIdBefore;
                await applicationFormSubmissionRepository.UpdateAsync(submission);
            }

            foreach (var agent in applicantAgents)
            {
                agent.ApplicantId = change.ApplicantIdBefore;
                await applicantAgentRepository.UpdateAsync(agent);
            }

            foreach (var address in applicantAddresses.Where(
                         item => relatedRecords.ApplicantAddressIds.Contains(item.Id)))
            {
                address.ApplicantId = change.ApplicantIdBefore;
                await applicantAddressRepository.UpdateAsync(address);
            }
        }

        Deserialize<ApplicantMergeApplicantSnapshot>(operation.PrincipalStateBefore).Restore(principal);
        Deserialize<ApplicantMergeApplicantSnapshot>(operation.SecondaryStateBefore).Restore(secondary);
        await applicantRepository.UpdateAsync(principal);
        await applicantRepository.UpdateAsync(secondary);

        operation.MarkReversed(reversedById, Clock.Now, reason);
        return await mergeOperationRepository.UpdateAsync(operation, autoSave: true);
    }

    private ApplicantMergeApplicationChange CreateApplicationChange(
        Guid mergeOperationId,
        Guid? tenantId,
        Application application,
        bool wasTransferred,
        Guid applicantIdBefore,
        Guid applicantIdAfter,
        ApplicantMergeRelatedRecordsSnapshot relatedRecords)
    {
        return new ApplicantMergeApplicationChange(
            GuidGenerator.Create(),
            tenantId,
            mergeOperationId,
            application.Id,
            wasTransferred,
            applicantIdBefore,
            applicantIdAfter,
            application.DefaultSiteId,
            null,
            Serialize(relatedRecords));
    }

    private async Task<bool> RelatedRecordsMatchAfterStateAsync(ApplicantMergeOperation operation)
    {
        foreach (var change in operation.ApplicationChanges)
        {
            var application = await applicationRepository.FindAsync(change.ApplicationId);
            if (application == null
                || application.ApplicantId != change.ApplicantIdAfter
                || application.DefaultSiteId != change.DefaultSiteIdAfter)
            {
                return false;
            }

            if (!change.WasTransferred)
            {
                continue;
            }

            var expected = Deserialize<ApplicantMergeRelatedRecordsSnapshot>(change.RelatedRecordsSnapshot);
            var formSubmissions = await applicationFormSubmissionRepository.GetListAsync(
                item => item.ApplicationId == change.ApplicationId && item.ApplicantId == change.ApplicantIdAfter);
            var applicantAgents = await applicantAgentRepository.GetListAsync(
                item => item.ApplicationId == change.ApplicationId && item.ApplicantId == change.ApplicantIdAfter);
            var applicantAddresses = await applicantAddressRepository
                .FindByApplicantIdAndApplicationIdAsync(change.ApplicantIdAfter, change.ApplicationId);

            if (!IdsMatch(formSubmissions.Select(item => item.Id), expected.ApplicationFormSubmissionIds)
                || !IdsMatch(applicantAgents.Select(item => item.Id), expected.ApplicantAgentIds)
                || !IdsMatch(applicantAddresses.Select(item => item.Id), expected.ApplicantAddressIds))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IdsMatch(IEnumerable<Guid> currentIds, IEnumerable<Guid> expectedIds)
    {
        return currentIds.ToHashSet().SetEquals(expectedIds);
    }

    private static bool ApplicantsAvailable(Applicant? principal, Applicant? secondary)
    {
        return principal is { IsDeleted: false } && secondary is { IsDeleted: false };
    }

    private static void EnsureApplicantsAvailable(Applicant? principal, Applicant? secondary)
    {
        if (!ApplicantsAvailable(principal, secondary))
        {
            throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeApplicantUnavailable);
        }
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, SnapshotJsonOptions);
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, SnapshotJsonOptions)
            ?? throw new BusinessException(GrantManagerDomainErrorCodes.ApplicantMergeInvalidHistory);
    }
}

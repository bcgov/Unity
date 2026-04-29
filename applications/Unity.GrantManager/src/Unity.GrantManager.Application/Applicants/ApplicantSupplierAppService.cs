using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Payments;
using Unity.Modules.Shared;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Suppliers;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applicants;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantSupplierAppService), typeof(IApplicantSupplierAppService))]
public class ApplicantSupplierAppService(ISiteRepository siteRepository,
                                 IApplicantRepository applicantRepository,
                                 IApplicationRepository applicationRepository,
                                 ISupplierService supplierService,
                                 ISupplierAppService supplierAppService,
                                 IPaymentRequestAppService paymentRequestService) : GrantManagerAppService, IApplicantSupplierAppService
{

    public async Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId)
    {
        return await siteRepository.GetBySupplierAsync(supplierId);
    }

    public async Task<dynamic> GetSupplierByNumber(string supplierNumber)
    {
        return await supplierService.GetCasSupplierInformationAsync(supplierNumber);
    }

    public async Task<dynamic> GetSupplierByBusinessNumber(string bn9)
    {
        return await supplierService.GetCasSupplierInformationByBn9Async(bn9);
    }

    /// <summary>
    /// Update the supplier number for the applicant regardless of application. 
    /// </summary>
    [Authorize(UnitySelector.Payment.Supplier.Update)]
    public async Task UpdateApplicantSupplierNumberAsync(Guid applicantId, string supplierNumber, Guid? applicationId = null)
    {
        if (await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature))
        {
            await applicantRepository.EnsureExistsAsync(applicantId);

            // Handle clearing supplier information
            if (string.IsNullOrEmpty(supplierNumber))
            {
                await ClearApplicantSupplierAsync(applicantId);
                return;
            }
        
            var supplier = await GetSupplierByApplicantIdAsync(applicantId);

            if (supplier != null && string.Compare(supplierNumber, supplier.Number, true) == 0)
            {
                return; // No change in supplier number, so no action needed
            }

            await supplierService.UpdateApplicantSupplierInfo(supplierNumber, applicantId, applicationId);
        }
    }

    [Authorize(UnitySelector.Payment.Supplier.Update)]
    public async Task ClearApplicantSupplierAsync(Guid applicantId)
    {
        if (await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature))
        {
            await applicantRepository.EnsureExistsAsync(applicantId);

            var applicant = await applicantRepository.GetAsync(applicantId);
            applicant.SupplierId = null;
            await applicantRepository.UpdateAsync(applicant);

            // Cascade: clear DefaultSiteId on every application — sites belong to the
            // cleared supplier so their references are no longer valid.
            var applications = await applicationRepository
                .GetListAsync(a => a.ApplicantId == applicantId);
            foreach (var application in applications)
            {
                application.DefaultSiteId = null;
                await applicationRepository.UpdateAsync(application);
            }
        }
    }

    [HttpPut("api/app/applicant/{applicantId}/bn9/{bn9}")]
    public async Task<dynamic> UpdateAplicantSupplierByBn9Async(Guid applicantId, string bn9)
    {
        await EnsureNoPendingPaymentsForApplicantAsync(applicantId);
        return await supplierService.UpdateApplicantSupplierInfoByBn9(bn9, applicantId);
    }

    [HttpPost("api/app/application/{applicationId}/site/{siteId}")]
    public async Task DefaultApplicationSite(Guid applicationId, Guid siteId)
    {
        Application application = await applicationRepository.GetAsync(applicationId);
        application.DefaultSiteId = siteId;
        await applicationRepository.UpdateAsync(application);
    }

    /// <summary>
    /// Throws UserFriendlyException if the applicant has any outstanding (L1Pending / L2Pending)
    /// payment request across any of their applications. No-op when the Unity Payments feature
    /// is disabled. Call this immediately before any supplier-number-mutating operation.
    /// </summary>
    public async Task EnsureNoPendingPaymentsForApplicantAsync(Guid applicantId)
    {
        if (!await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature))
        {
            return;
        }

        var applicationIds = (await applicationRepository
            .GetListAsync(a => a.ApplicantId == applicantId))
            .Select(a => a.Id)
            .ToList();

        if (applicationIds.Count == 0)
        {
            return;
        }

        var pendingPayments = await paymentRequestService
            .GetPaymentPendingListByCorrelationIdsAsync(applicationIds);

        if (pendingPayments != null && pendingPayments.Count > 0)
        {
            throw new UserFriendlyException(
                "This applicant has outstanding payment requests on one or more applications. " +
                "Please decline or approve them before changing the Supplier Number.");
        }
    }

    public async Task<SupplierDto?> GetSupplierByApplicantIdAsync(Guid applicantId)
    {
        Applicant applicant = await applicantRepository.GetAsync(applicantId);
        if (!applicant.SupplierId.HasValue) return null;
        return await supplierAppService.GetAsync(applicant.SupplierId.Value);
    }

    [HttpGet("api/app/applicant-supplier/has-pending-payments-for-merge")]
    public async Task<bool> HasPendingPaymentsForMergeAsync(Guid principalId, Guid nonPrincipalId)
    {
        if (!await FeatureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature))
        {
            return false;
        }

        var principalAppIds = (await applicationRepository
            .GetListAsync(a => a.ApplicantId == principalId))
            .Select(a => a.Id)
            .ToList();

        var nonPrincipalAppIds = (await applicationRepository
            .GetListAsync(a => a.ApplicantId == nonPrincipalId))
            .Select(a => a.Id)
            .ToList();

        var allAppIds = principalAppIds.Concat(nonPrincipalAppIds).ToList();
        if (allAppIds.Count == 0) return false;

        var pendingPayments = await paymentRequestService
            .GetPaymentPendingListByCorrelationIdsAsync(allAppIds);

        return pendingPayments != null && pendingPayments.Count > 0;
    }

    [HttpPost("api/app/applicant-supplier/handle-supplier-after-merge")]
    [Authorize(UnitySelector.Payment.Supplier.Update)]
    public async Task HandleSupplierAfterMergeAsync(HandleSupplierAfterMergeDto dto)
    {
        await EnsureNoPendingPaymentsForApplicantAsync(dto.PrincipalId);
        await EnsureNoPendingPaymentsForApplicantAsync(dto.NonPrincipalId);

        var principal = await applicantRepository.GetAsync(dto.PrincipalId);
        principal.SupplierId = dto.SelectedSupplierId;
        await applicantRepository.UpdateAsync(principal);

        var nonPrincipal = await applicantRepository.GetAsync(dto.NonPrincipalId);
        nonPrincipal.SupplierId = dto.SelectedSupplierId;
        await applicantRepository.UpdateAsync(nonPrincipal);

        // Null DefaultSiteId on all applications now belonging to the principal
        // (both transferred and pre-existing). Staff re-set per application.
        var applications = await applicationRepository
            .GetListAsync(a => a.ApplicantId == dto.PrincipalId);
        foreach (var application in applications)
        {
            application.DefaultSiteId = null;
            await applicationRepository.UpdateAsync(application);
        }
    }
}

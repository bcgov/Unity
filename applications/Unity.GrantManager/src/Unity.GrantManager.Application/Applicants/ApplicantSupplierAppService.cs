using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Payments;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Correlation;
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
            var supplierId = applicant.SupplierId;

            // Clear the applicant-level supplier reference first.
            applicant.SupplierId = null;
            await applicantRepository.UpdateAsync(applicant);

            // Cascade: clear DefaultSiteId on every application of this applicant.
            // Sites belong to the cleared supplier, so the references are no longer valid.
            var applications = await applicationRepository
                .GetListAsync(a => a.ApplicantId == applicantId);
            foreach (var application in applications)
            {
                application.DefaultSiteId = null;
                await applicationRepository.UpdateAsync(application);
            }

            if (supplierId.HasValue)
            {
                await supplierAppService.ClearCorrelationAsync(supplierId.Value);
            }
            else
            {
                // Handle existing data where SupplierId was already cleared
                // but the supplier's correlation was never removed
                var supplier = await supplierAppService.GetByCorrelationAsync(
                    new GetSupplierByCorrelationDto()
                    {
                        CorrelationId = applicantId,
                        CorrelationProvider = CorrelationConsts.Applicant,
                        IncludeDetails = false
                    });

                if (supplier != null)
                {
                    await supplierAppService.ClearCorrelationAsync(supplier.Id);
                }
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

        //If SupplierId is available, use it to get the supplier
        if (applicant.SupplierId.HasValue)
        {
            Guid supplierId = applicant.SupplierId.Value;
            return await supplierAppService.GetAsync(supplierId);
        }

        // If no SupplierId, fetch the supplier using the correlation
        return await supplierAppService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
        {
            CorrelationId = applicantId,
            CorrelationProvider = CorrelationConsts.Applicant,
            IncludeDetails = true
        });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Payments;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Correlation;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Integrations.Cas;
using Unity.Payments.Suppliers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applicants;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantSupplierAppService), typeof(IApplicantSupplierAppService))]
public class ApplicantSupplierAppService(ISiteRepository siteRepository,
                                 IApplicantRepository applicantRepository,
                                 ISupplierService supplierService,
                                 ISupplierAppService supplierAppService) : GrantManagerAppService, IApplicantSupplierAppService
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

            if (supplier != null && string.Compare(supplierNumber, supplier?.Number, true) == 0)
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
            var supplierId = applicant.SupplierId; // Store the supplier ID before clearing
            
            // Clear the applicant references first
            applicant.SupplierId = null;
            applicant.SiteId = null;
            await applicantRepository.UpdateAsync(applicant);

             if (supplierId.HasValue)
            {
                await supplierAppService.DeleteAsync(supplierId.Value);
            }
        }
    }

    [HttpPut("api/app/applicant/{applicantId}/bn9/{bn9}")]
    public async Task<dynamic> UpdateAplicantSupplierByBn9Async(Guid applicantId, string bn9)
    {
        return await supplierService.UpdateApplicantSupplierInfoByBn9(bn9, applicantId);
    }

    [HttpPost("api/app/applicant/{applicantId}/site/{siteId}")]
    public async Task DefaultApplicantSite(Guid applicantId, Guid siteId)
    {
        Applicant applicant = await applicantRepository.GetAsync(applicantId);
        applicant.SiteId = siteId;
        await applicantRepository.UpdateAsync(applicant);
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

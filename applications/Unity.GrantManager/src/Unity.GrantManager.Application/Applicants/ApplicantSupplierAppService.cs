using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using System;
using Unity.Payments.Suppliers;
using Unity.Modules.Shared.Correlation;
using System.Collections.Generic;
using Unity.Payments.Domain.Suppliers;
using Microsoft.AspNetCore.Mvc;
using Unity.Payments.Integrations.Cas;

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
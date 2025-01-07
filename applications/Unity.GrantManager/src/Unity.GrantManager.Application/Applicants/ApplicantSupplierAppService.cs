using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.DependencyInjection;
using System;
using Unity.Payments.Suppliers;
using Unity.Modules.Shared.Correlation;
using System.Collections.Generic;
using Unity.Payments.Domain.Suppliers;

namespace Unity.GrantManager.Applicants;


[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicantSupplierAppService), typeof(IApplicantSupplierAppService))]
public class ApplicantSupplierAppService(ISiteRepository siteRepository,
                                 IApplicantRepository applicantRepository,
                                 ISupplierAppService supplierAppService) : GrantManagerAppService, IApplicantSupplierAppService
{

    public async Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId)
    {
        return await siteRepository.GetBySupplierAsync(supplierId);
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
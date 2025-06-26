using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Suppliers;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applicants;

public interface IApplicantSupplierAppService : IApplicationService
{
    Task<dynamic> GetSupplierByNumber(string supplierNumber);
    Task<SupplierDto?> GetSupplierByApplicantIdAsync(Guid applicantId);
    Task<List<Site>> GetSitesBySupplierIdAsync(Guid supplierId);
    Task UpdateApplicantSupplierNumberAsync(Guid applicantId, string supplierNumber);
    Task<dynamic> UpdateAplicantSupplierByBn9Async(Guid applicantId, string bn9);
}

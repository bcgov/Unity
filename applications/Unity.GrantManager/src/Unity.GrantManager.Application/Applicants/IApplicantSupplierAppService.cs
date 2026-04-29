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
    Task ClearApplicantSupplierAsync(Guid applicantId);
    Task UpdateApplicantSupplierNumberAsync(Guid applicantId, string supplierNumber, Guid? applicationId = null);
    Task<dynamic> UpdateAplicantSupplierByBn9Async(Guid applicantId, string bn9);
    Task EnsureNoPendingPaymentsForApplicantAsync(Guid applicantId);

    /// <summary>
    /// Returns true if either the principal or non-principal applicant has any
    /// in-progress payments (L1Pending / L2Pending / L3Pending). Used by the
    /// Merge UI to show a warning before the merge is executed.
    /// </summary>
    Task<bool> HasPendingPaymentsForMergeAsync(Guid principalId, Guid nonPrincipalId);

    /// <summary>
    /// Applies supplier and DefaultSiteId changes after a merge:
    /// sets SupplierId = selectedSupplierId on both the principal and non-principal
    /// (keeping the duplicate record in sync), and nulls DefaultSiteId on every
    /// application that now belongs to the principal.
    /// </summary>
    Task HandleSupplierAfterMergeAsync(HandleSupplierAfterMergeDto dto);
}

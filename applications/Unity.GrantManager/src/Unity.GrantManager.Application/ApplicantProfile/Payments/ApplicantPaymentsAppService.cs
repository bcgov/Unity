using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Payments;
using Unity.Modules.Shared;
using Unity.Payments.Codes;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Features;

namespace Unity.GrantManager.ApplicantProfile;

[RequiresFeature(PaymentConsts.UnityPaymentsFeature)]
[Authorize]
public class ApplicantPaymentsAppService(
    IApplicationRepository applicationRepository,
    IPaymentRequestAppService paymentRequestAppService) : GrantManagerAppService, IApplicantPaymentsAppService
{
    [Authorize(UnitySelector.Payment.Summary.Default)]
    public async Task<ApplicantPaymentSummaryDto> GetPaymentSummaryByApplicantIdAsync(Guid applicantId)
    {
        var applications = await applicationRepository.GetByApplicantIdAsync(applicantId);
        var totalApproved = applications.Sum(a => a.ApprovedAmount);

        if (applications.Count == 0)
            return new ApplicantPaymentSummaryDto { TotalApprovedAmount = totalApproved };

        var applicationIds = applications.Select(a => a.Id).ToList();
        var payments = await paymentRequestAppService.GetListByApplicationIdsAsync(applicationIds);
        var totalPaid = payments
            .Where(p => p.Status == PaymentRequestStatus.HistoricalPayment
                     || string.Equals(p.PaymentStatus?.Trim(), CasPaymentRequestStatus.FullyPaid, StringComparison.OrdinalIgnoreCase))
            .Sum(p => p.Amount);

        return new ApplicantPaymentSummaryDto
        {
            TotalApprovedAmount = totalApproved,
            TotalPaidAmount = totalPaid,
            TotalRemainingAmount = totalApproved - totalPaid
        };
    }

    [Authorize(UnitySelector.Payment.PaymentList.Default)]
    public async Task<List<ApplicantPaymentDetailsDto>> GetPaymentListByApplicantIdAsync(Guid applicantId)
    {
        var applications = await applicationRepository.GetByApplicantIdAsync(applicantId);

        if (applications.Count == 0) return [];

        var referenceMap = applications.ToDictionary(a => a.Id, a => a.ReferenceNo);
        var categoryMap = applications.ToDictionary(a => a.Id, a => a.ApplicationForm?.Category ?? string.Empty);
        var applicationIds = applications.Select(a => a.Id).ToList();
        var payments = await paymentRequestAppService.GetListByApplicationIdsAsync(applicationIds);

        return payments.Select(p => new ApplicantPaymentDetailsDto
        {
            Id = p.Id,
            ReferenceNumber = p.ReferenceNumber,
            ApplicationReferenceNo = referenceMap.TryGetValue(p.CorrelationId, out var refNo) ? refNo : string.Empty,
            ApplicationId = p.CorrelationId,
            PaymentDate = p.PaymentDate,
            Status = p.Status,
            Amount = p.Amount,
            PaymentStatus = p.PaymentStatus,
            InvoiceStatus = p.InvoiceStatus,
            CasResponse = p.CasResponse,
            Category = categoryMap.TryGetValue(p.CorrelationId, out var cat) ? cat : string.Empty,
            SupplierNumber = p.SupplierNumber,
            SupplierName = p.SupplierName,
            Site = p.Site
        }).ToList();
    }
}

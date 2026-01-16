using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.PaymentRequests;

namespace Unity.Payments.Domain.Services
{
    public interface IPaymentRequestQueryManager
    {
        // Payment Request Queries
        Task<int> GetPaymentRequestCountBySiteIdAsync(Guid siteId);
        Task<long> GetPaymentRequestCountAsync();
        Task<PaymentRequest?> GetPaymentRequestByIdAsync(Guid paymentRequestId);
        Task<List<PaymentRequest>> GetPaymentRequestsByIdsAsync(List<Guid> paymentRequestIds, bool includeDetails = false);
        Task<List<PaymentRequest>> GetPagedPaymentRequestsWithIncludesAsync(int skipCount, int maxResultCount, string sorting);
        Task<List<PaymentDetailsDto>> GetListByApplicationIdAsync(Guid applicationId);
        Task<List<PaymentDetailsDto>> GetListByApplicationIdsAsync(List<Guid> applicationIds);
        Task<List<PaymentDetailsDto>> GetListByPaymentIdsAsync(List<Guid> paymentIds);
        Task<decimal> GetTotalPaymentRequestAmountByCorrelationIdAsync(Guid correlationId);

        // Payment Request Operations
        Task<PaymentRequest> InsertPaymentRequestAsync(PaymentRequest paymentRequest);

        // DTO Creation & Mapping
        Task<PaymentRequestDto> CreatePaymentRequestDtoAsync(Guid paymentRequestId);
        Task<List<PaymentRequestDto>> MapToDtoAndLoadDetailsAsync(List<PaymentRequest> paymentsList);
        Task<string> GetAccountDistributionCodeAsync(AccountCodingDto? accountCoding);

        // Queue Operations
        Task ManuallyAddPaymentRequestsToReconciliationQueueAsync(List<Guid> paymentRequestIds);

        // Helper Method
        void ApplyErrorSummary(List<PaymentRequestDto> mappedPayments);

        // Pending Payments
        Task<List<PaymentRequestDto>> GetPaymentPendingListByCorrelationIdAsync(Guid applicationId);
    }
}

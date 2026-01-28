using System;
using System.Threading.Tasks;
using Unity.Payments.Domain.PaymentConfigurations;

namespace Unity.Payments.Domain.Services
{
    public interface IPaymentRequestConfigurationManager
    {
        // Configuration & Lookup
        Task<Guid?> GetDefaultAccountCodingIdAsync();
        Task<PaymentConfiguration?> GetPaymentConfigurationAsync();
        Task<string> GetNextBatchInfoAsync();
        Task<decimal> GetMaxBatchNumberAsync();

        // Threshold & Approval Logic
        Task<decimal?> GetPaymentRequestThresholdByApplicationIdAsync(Guid applicationId, decimal? userPaymentThreshold);
        Task<decimal?> GetUserPaymentThresholdAsync(Guid? userId);

        // Utility Methods for Batch/Sequence Generation
        Task<int> GetNextSequenceNumberAsync(int currentYear);
        string GenerateReferenceNumberPrefix(string paymentIdPrefix);
        string GenerateSequenceNumber(int sequenceNumber, int index);
        string GenerateReferenceNumber(string referenceNumber, string sequencePart);
        string GenerateInvoiceNumber(string referenceNumber, string invoiceNumber, string sequencePart);
    }
}

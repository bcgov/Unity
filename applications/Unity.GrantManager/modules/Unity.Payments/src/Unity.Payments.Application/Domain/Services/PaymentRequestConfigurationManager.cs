using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentThresholds;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace Unity.Payments.Domain.Services
{
    public class PaymentRequestConfigurationManager(
        IApplicationRepository applicationRepository,
        IApplicationFormRepository applicationFormRepository,
        IPaymentConfigurationRepository paymentConfigurationRepository,
        IPaymentThresholdRepository paymentThresholdRepository,
        IPaymentRequestRepository paymentRequestRepository) : DomainService, IPaymentRequestConfigurationManager
    {
        public async Task<Guid?> GetDefaultAccountCodingIdAsync()
        {
            Guid? accountCodingId = null;
            // If no account coding is found look up the payment configuration
            PaymentConfiguration? paymentConfiguration = await GetPaymentConfigurationAsync();
            if (paymentConfiguration != null && paymentConfiguration.DefaultAccountCodingId.HasValue)
            {
                accountCodingId = paymentConfiguration.DefaultAccountCodingId;
            }
            return accountCodingId;
        }

        public async Task<string> GetNextBatchInfoAsync()
        {
            var paymentConfig = await GetPaymentConfigurationAsync();
            var paymentIdPrefix = string.Empty;

            if (paymentConfig != null && !paymentConfig.PaymentIdPrefix.IsNullOrEmpty())
            {
                paymentIdPrefix = paymentConfig.PaymentIdPrefix;
            }

            var batchNumber = await GetMaxBatchNumberAsync();
            var batchName = $"{paymentIdPrefix}_UNITY_BATCH_{batchNumber}";

            return batchName;
        }

        public string GenerateInvoiceNumber(string referenceNumber, string invoiceNumber, string sequencePart)
        {
            return $"{referenceNumber}-{invoiceNumber}-{sequencePart}";
        }

        public string GenerateReferenceNumber(string referenceNumber, string sequencePart)
        {
            return $"{referenceNumber}-{sequencePart}";
        }

        public string GenerateSequenceNumber(int sequenceNumber, int index)
        {
            sequenceNumber += index;
            return sequenceNumber.ToString("D4");
        }

        public string GenerateReferenceNumberPrefix(string paymentIdPrefix)
        {
            var currentYear = DateTime.UtcNow.Year;
            var yearPart = currentYear.ToString();
            return $"{paymentIdPrefix}-{yearPart}";
        }

        public async Task<decimal> GetMaxBatchNumberAsync()
        {
            var paymentRequestList = await paymentRequestRepository.GetListAsync();
            decimal batchNumber = 1; // Lookup max plus 1
            if (paymentRequestList != null && paymentRequestList.Count > 0)
            {
                var maxBatchNumber = paymentRequestList.Max(s => s.BatchNumber);

                if (maxBatchNumber > 0)
                {
                    batchNumber = maxBatchNumber + 1;
                }
            }

            return batchNumber;
        }

        public async Task<decimal?> GetPaymentRequestThresholdByApplicationIdAsync(Guid applicationId, decimal? userPaymentThreshold)
        {
            var application = await (await applicationRepository.GetQueryableAsync())
            .Include(a => a.ApplicationForm)
            .FirstOrDefaultAsync(a => a.Id == applicationId) ?? throw new BusinessException($"Application with Id {applicationId} not found.");
            var appForm = application.ApplicationForm ??
            (application.ApplicationFormId != Guid.Empty
                ? await applicationFormRepository.GetAsync(application.ApplicationFormId)
                : null);

            var formThreshold = appForm?.PaymentApprovalThreshold;

            if (formThreshold.HasValue && userPaymentThreshold.HasValue)
            {
                return Math.Min(formThreshold.Value, userPaymentThreshold.Value);
            }

            return formThreshold ?? userPaymentThreshold ?? 0m;
        }

        public async Task<decimal?> GetUserPaymentThresholdAsync(Guid? userId)
        {
            var userThreshold = await paymentThresholdRepository.FirstOrDefaultAsync(x => x.UserId == userId);
            return userThreshold?.Threshold;
        }

        public async Task<PaymentConfiguration?> GetPaymentConfigurationAsync()
        {
            var paymentConfigs = await paymentConfigurationRepository.GetListAsync();

            if (paymentConfigs.Count > 0)
            {
                var paymentConfig = paymentConfigs[0];
                return paymentConfig;
            }

            return null;
        }

        public async Task<int> GetNextSequenceNumberAsync(int currentYear)
        {
            // Retrieve all payment requests
            var payments = await paymentRequestRepository.GetListAsync();

            // Filter payments for the current year
            var filteredPayments = payments
                .Where(p => p.CreationTime.Year == currentYear)
                .OrderByDescending(p => p.CreationTime)
                .ToList();

            // Use the first payment in the sorted list (most recent) if available
            if (filteredPayments.Count > 0)
            {
                var latestPayment = filteredPayments[0]; // Access the most recent payment directly
                var referenceParts = latestPayment.ReferenceNumber.Split('-');

                // Extract the sequence number from the reference number safely
                if (referenceParts.Length > 0 && int.TryParse(referenceParts[^1], out int latestSequenceNumber))
                {
                    return latestSequenceNumber + 1;
                }
            }

            // If no payments exist or parsing fails, return the initial sequence number
            return 1;
        }
    }
}
